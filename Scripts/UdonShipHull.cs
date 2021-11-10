
using System;
using TMPro;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(AudioSource))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonShipHull : UdonSharpBehaviour
    {
        public Transform centerOfMass;
        public Vector3 extents = Vector3.one;
        public TextMeshPro ownerText;

        private new Rigidbody rigidbody;
        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
            rigidbody.useGravity = true;
            UpdateParameters();
        }

        private void FixedUpdate()
        {
            var velocity = rigidbody.velocity;
            var angularVelocity = rigidbody.angularVelocity;
            var position = transform.position;
            var gravity = Physics.gravity;


            for (int i = 0; i < compartmentCount; i++) {
                // if (dead) flood[i] = Mathf.Clamp01(flood[i] + Time.fixedDeltaTime * Random.Range(0.0f, 0.1f));
                // else flood[i] = 0;

                var p = GetCompartmentPosition(compartments[i]);
                var d = GetCompartmenDepth(p);
                var v = GetCompartmentVelocity(velocity, angularVelocity, position, p);
                var force = GetCompartmentBuoyancy(d, gravity, flood[i]) + GetCompartmentDrag(v, d);
                rigidbody.AddForceAtPosition(Vector3.ClampMagnitude(force, maxForce), GetCompartmentBuoyancyCenter(p, d), ForceMode.Force);
            }
        }

        #region Physics
        [SectionHeader("Water Phisics")]

        public Vector3[] compartments = {
            Vector3.zero,
            Vector3.right * 0.75f,
            Vector3.left * 0.75f,
            Vector3.forward * 0.75f,
            Vector3.back * 0.75f,
        };

        [HelpBox("Proportional to pow(V, 1). aka. Viscous resistance")] public Vector3 frictionDragCoefficient = new Vector3(0f, 0f, 0f);
        [HelpBox("Proportional to pow(V, 2). aka. Inertial resistance")] public Vector3 pressureDragCoefficient = new Vector3(0.8f, 1.0f, .3f);
        [HelpBox("Proportional to pow(V, 3)")] public Vector3 waveDragCoefficient = new Vector3(0f, 0f, 0f);

        [NonSerialized] public float waterDensity = 0.99997495f;
        [NonSerialized] public float waterViscosity = 0.000890f;
        [NonSerialized] public float maxForce = 1000;
        [NonSerialized] public float seaHeight;

        private float volume, compartmentVolume, compartmentSideArea, compartmentBottomArea;
        private int compartmentCount;
        private Vector3 size, compartmentCrossArea;
        private float[] flood;
        public void UpdateParameters()
        {
            if (centerOfMass != null) rigidbody.centerOfMass = rigidbody.transform.InverseTransformPoint(centerOfMass.position);

            size = extents * 2.0f;
            volume = size.x * size.y * size.z;

            compartmentCount = compartments.Length;
            compartmentVolume = volume / compartmentCount;

            var sideArea = size.x * size.y * 2.0f + size.y * size.z * 2.0f;
            compartmentSideArea = sideArea / compartmentCount;
            var bottomArea = size.x * size.z;
            compartmentBottomArea = bottomArea / compartmentCount;

            var crossArea = new Vector3(size.y * size.z, size.x * size.z, size.x * size.y);
            compartmentCrossArea = crossArea / compartmentCount;

            flood = new float[compartmentCount];
            for (int i = 0; i < compartmentCount; i++)
            {
                flood[i] = 0.0f;
            }
        }

        private Vector3 GetCompartmentPosition(Vector3 compartment)
        {
            return transform.TransformPoint(Vector3.Scale(compartment, extents));
        }

        private float GetCompartmenDepth(Vector3 position)
        {
            return Mathf.Clamp(extents.y - position.y + seaHeight , 0, size.y);
        }

        private Vector3 GetCompartmentBuoyancy(float depth, Vector3 gravity, float flood)
        {
            var underWaterVolume = compartmentVolume * depth / size.y;
            return -gravity * underWaterVolume * waterDensity * (1.0f - flood);
        }

        private Vector3 GetCompartmentBuoyancyCenter(Vector3 compartmentPosition, float depth)
        {
            return compartmentPosition + Vector3.down * Mathf.Max(extents.y - depth * 0.5f, 0.0f);
        }

        private Vector3 GetCompartmentVelocity(Vector3 velocity, Vector3 angularVelocity, Vector3 position, Vector3 compartmentPosition)
        {
            return velocity + Vector3.Cross(angularVelocity, compartmentPosition - position);
        }

        private Vector3 GetCompartmentDrag(Vector3 velocity, float depth)
        {
            var localVelocity = transform.InverseTransformVector(velocity);

            var sqrLocalSpeed = localVelocity.sqrMagnitude;
            if (sqrLocalSpeed < 0.0001f) return Vector3.zero;

            var localSpeed = Mathf.Sqrt(sqrLocalSpeed);
            var cubeLocalSpeed = localSpeed * sqrLocalSpeed;

            var localVelocityDirection = localVelocity / localSpeed;

            var underWaterRatio = Mathf.Clamp01(depth / size.y);
            var underWaterSurfaceArea = compartmentSideArea * underWaterRatio + compartmentBottomArea;
            var underWaterCrossArea = compartmentCrossArea * underWaterRatio;

            var waterDV = waterDensity * waterViscosity;

            return transform.TransformVector(
                - Vector3.Scale(localVelocityDirection, frictionDragCoefficient) * waterViscosity * underWaterSurfaceArea * localSpeed * 2.0f
                - Vector3.Scale(Vector3.Scale(localVelocityDirection, pressureDragCoefficient), underWaterCrossArea) * waterDV * sqrLocalSpeed * 2.0f
                - Vector3.Scale(Vector3.Scale(localVelocityDirection, waveDragCoefficient), underWaterCrossArea) * waterDV * cubeLocalSpeed * 2.0f
            );
        }
        #endregion

        #region Damage
        // [SectionHeader("Damage")]
        // public GameObject deadEffect;
        // public bool capsizing = true;
        // [Range(0, 1.0f)] public float capsizingThreshold = 0.9f;
        // private void Update()
        // {
        //     if (!Networking.IsOwner(gameObject)) return;

        //     if (capsizing && Vector3.Dot(transform.up, Vector3.down) > capsizingThreshold) Capsized();
        // }

        // private GameObject spawnedDeadEffect;
        // private bool dead;
        // private bool ignoreDamage;
        // public void ResetDead()
        // {
        //     dead = false;
        //     ignoreDamage = false;
        //     if (Utilities.IsValid(spawnedDeadEffect)) Destroy(spawnedDeadEffect);
        // }

        // public void IgnoreDamage(float duration)
        // {
        //     ignoreDamage = true;
        //     SendCustomEventDelayedSeconds(nameof(EnableDamage), duration);
        // }

        // public void EnableDamage()
        // {
        //     ignoreDamage = false;
        // }

        // public void Dead()
        // {
        //     dead = true;
        //     ignoreDamage = false;
        //     if (!Utilities.IsValid(spawnedDeadEffect) && deadEffect != null)
        //     {
        //         spawnedDeadEffect = VRCInstantiate(deadEffect);
        //         spawnedDeadEffect.transform.parent = transform;
        //         spawnedDeadEffect.transform.localPosition = Vector3.zero;
        //     }
        // }

        // private void Capsized()
        // {
        //     if (!ignoreDamage) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Dead));
        // }

        // public void BulletHit()
        // {
        //     if (Random.value <= 0.5f && !ignoreDamage) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Dead));
        // }
        #endregion

        // #region Collision Damage
        // [SectionHeader("Collision Damage")]
        // public float collisionDamage = 1.0f;
        // private void OnCollisionEnter(Collision collision)
        // {
        //     if (collision == null || dead || ignoreDamage) return;
        //     if (Random.Range(0, collision.relativeVelocity.sqrMagnitude * collisionDamage) >= 1.0f)
        //     {
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(CollisionDamaged));
        //     }
        // }

        // public AudioClip collisionSound;
        // public void CollisionDamaged()
        // {
        //     if (collisionSound != null) GetComponent<AudioSource>().PlayOneShot(collisionSound);
        //     Dead();
        // }
        // #endregion

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rigidbody == null) rigidbody = GetComponentInParent<Rigidbody>();
            var velocity = rigidbody.velocity;
            var angularVelocity = rigidbody.angularVelocity;

            for (int i = 0; i < compartmentCount; i++) {
                var compartmentPosition = GetCompartmentPosition(compartments[i]);
                var compartmentDepth = GetCompartmenDepth(compartmentPosition);
                var compartmentVelocity = GetCompartmentVelocity(velocity, angularVelocity, transform.position, compartmentPosition);

                var buoyancy = GetCompartmentBuoyancy(compartmentDepth, Physics.gravity, flood[i]);
                var drag = GetCompartmentDrag(compartmentVelocity, compartmentDepth);
                var buoyancyCenter = GetCompartmentBuoyancyCenter(compartmentPosition, compartmentDepth);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(buoyancyCenter, buoyancyCenter + buoyancy / rigidbody.mass);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(buoyancyCenter, buoyancyCenter + drag / rigidbody.mass);
            }

            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, extents * 2.0f);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
