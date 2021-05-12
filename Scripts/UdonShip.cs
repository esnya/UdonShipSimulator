
using TMPro;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(AudioSource))]
    public class UdonShip : UdonSharpBehaviour
    {
        public Transform centerOfMass;
        public Transform rudder;
        public float rudderCoefficient = 0.0001f;
        public float rudderBackwardCoefficient = 0.00005f;
        [ListView("Thrusters/Screws")] public Transform[] thrusters = { };
        [ListView("Thrusters/Screws")] public float[] thrustForces = { 0.0001f };
        [HideInInspector] public float[] thrustPowers;
        public Vector3 extents;
        public TextMeshPro ownerText;

        private new Rigidbody rigidbody;
        private VRCPickup pickup;
        private int thrusterCount;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.useGravity = true;
            if (centerOfMass != null) rigidbody.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));

            thrusterCount = Mathf.Min(thrusters.Length, thrustForces.Length);
            thrustPowers = new float[thrusterCount];

            UpdateParameters();

            OnOwnershipTransferred();
        }

        private bool GetIsHeldSelf()
        {
            return pickup != null && pickup.IsHeld;
        }

        private void FixedUpdate()
        {
            if (!Networking.IsOwner(gameObject) || rigidbody.IsSleeping() || GetIsHeldSelf()) return;

            var velocity = rigidbody.velocity;
            var angularVelocity = rigidbody.angularVelocity;
            var position = transform.position;

            for (int i = 0; i < compartmentCount; i++) {
                if (dead) flood[i] = Mathf.Clamp01(flood[i] + Time.fixedDeltaTime * Random.Range(0.1f, 0.2f));
                else flood[i] = 0;

                var p = GetCompartmentPosition(compartments[i]);
                var d = GetCompartmenDepth(p);
                var v = GetCompartmentVelocity(velocity, angularVelocity, position, p);
                var force = Vector3.up * GetCompartmentBuoyancy(d, flood[i]) + GetCompartmentDrag(v, d);
                rigidbody.AddForceAtPosition(force, GetCompartmentBuoyancyCenter(p, d), ForceMode.Force);
            }

            if (!dead)
            {
                for (int i = 0; i < thrusterCount; i++)
                {
                    var thruster = thrusters[i];
                    if (thruster.position.y >= 0) continue;

                    var power = thrustForces[i] * thrustPowers[i];
                    rigidbody.AddForceAtPosition(thruster.forward * power, thruster.position, ForceMode.Force);
                }
            }

            var localVelocity = transform.InverseTransformVector(velocity);
            var cl = Vector3.Dot(velocity.normalized, rudder.right);
            var l = -0.5f * waterDensity * rigidbody.velocity.sqrMagnitude * (localVelocity.z >= 0 ? rudderCoefficient : rudderBackwardCoefficient) * cl;
            rigidbody.AddForceAtPosition(rudder.right * l, rudder.position, ForceMode.Force);
        }

        public override void OnOwnershipTransferred()
        {
            if (ownerText != null) ownerText.text = Networking.GetOwner(gameObject).displayName;
        }

        public void SetThrustPower(float power)
        {
            for (int i = 0; i < thrusterCount; i++) thrustPowers[i] = Mathf.Clamp(power, -1.0f, 1.0f);
        }

        private bool GetIsHeld(GameObject obj)
        {
            var pickup = (VRCPickup)obj.GetComponent(typeof(VRCPickup));
            if (pickup == null) return false;
            return pickup.IsHeld;
        }
        public void Respawn()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            rigidbody.ResetInertiaTensor();
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetDead));
            OnOwnershipTransferred();
        }

        public override void OnPickup()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetDead));
            OnOwnershipTransferred();
        }

        public override void OnDrop()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetDead));
            IgnoreDamage(5.0f);
        }

        #region Physics
        [SectionHeader("Water Phisics")]
        public float waterDensity = 0.99997495f;
        public float waterViscosity = 0.000890f;

        public Vector3[] compartments = {
            Vector3.zero,
            Vector3.right * 0.75f,
            Vector3.left * 0.75f,
            Vector3.forward * 0.75f,
            Vector3.back * 0.75f,
        };

        [HelpBox("Proportional to pow(V, 1). aka. Viscous resistance")] public Vector3 frictionDragScale = new Vector3(1f, 1f, 1f);
        [HelpBox("Proportional to pow(V, 2). aka. Inertial resistance")] public Vector3 pressureDragScale = new Vector3(10f, 10f, 5f);
        [HelpBox("Proportional to pow(V, 3)")] public Vector3 waveDragScale = new Vector3(10f, 10f, 5f);

        private float volume, compartmentVolume, compartmentSideArea, compartmentBottomArea;
        private int compartmentCount;
        private Vector3 size, compartmentCrossArea;
        private float[] flood;
        public void UpdateParameters()
        {
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
            return Mathf.Clamp(extents.y - position.y , 0, size.y);
        }

        private float GetCompartmentBuoyancy(float depth, float flood)
        {
            var underWaterVolume = compartmentVolume * depth / size.y;
            return underWaterVolume * waterDensity * (1.0f - flood);
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
                - Vector3.Scale(localVelocityDirection, frictionDragScale) * waterViscosity * underWaterSurfaceArea * localSpeed
                - Vector3.Scale(Vector3.Scale(localVelocityDirection, pressureDragScale), underWaterCrossArea) * waterDV * sqrLocalSpeed
                - Vector3.Scale(Vector3.Scale(localVelocityDirection, waveDragScale), underWaterCrossArea) * waterDV * cubeLocalSpeed
            );
        }
        #endregion

        #region Damage
        [SectionHeader("Damage")]
        public GameObject deadEffect;
        [Range(0, 1.0f)] public float capsizingThreshold = 0.9f;
        private void Update()
        {
            if (!Networking.IsOwner(gameObject)) return;

            if (Vector3.Dot(transform.up, Vector3.down) > capsizingThreshold) Capsized();
        }

        private GameObject spawnedDeadEffect;
        private bool dead;
        private bool ignoreDamage;
        public void ResetDead()
        {
            dead = false;
            ignoreDamage = false;
            if (Utilities.IsValid(spawnedDeadEffect)) Destroy(spawnedDeadEffect);
        }

        public void IgnoreDamage(float duration)
        {
            ignoreDamage = true;
            SendCustomEventDelayedSeconds(nameof(EnableDamage), duration);
        }

        public void EnableDamage()
        {
            ignoreDamage = false;
        }

        public void Dead()
        {
            dead = true;
            ignoreDamage = false;
            if (!Utilities.IsValid(spawnedDeadEffect) && deadEffect != null)
            {
                spawnedDeadEffect = VRCInstantiate(deadEffect);
                spawnedDeadEffect.transform.parent = transform;
                spawnedDeadEffect.transform.localPosition = Vector3.zero;
            }
        }

        private void Capsized()
        {
            if (!ignoreDamage) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Dead));
        }

        public void BulletHit()
        {
            if (Random.value <= 0.5f && !ignoreDamage) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Dead));
        }
        #endregion

        #region Collision Damage
        [SectionHeader("Collision Damage")]
        public float collisionDamage = 1.0f;
        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || !Networking.IsOwner(gameObject) || GetIsHeld(gameObject) || GetIsHeld(collision.gameObject) || dead || ignoreDamage) return;
            if (Random.Range(0, collision.relativeVelocity.sqrMagnitude * collisionDamage) >= 1.0f)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(CollisionDamaged));
            }
        }

        public AudioClip collisionSound;
        public void CollisionDamaged()
        {
            if (collisionSound != null) GetComponent<AudioSource>().PlayOneShot(collisionSound);
            Dead();
        }
        #endregion

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void DrawGravityGizmo(Vector3 p, float d, float scale)
        {
            Gizmos.DrawRay(GetCompartmentBuoyancyCenter(p, d), Physics.gravity * GetComponent<Rigidbody>().mass * scale);
        }
        private void DrawBuoyancyGizmo(Vector3 p, float d, float buoyancy, float scale)
        {
            Gizmos.DrawRay(GetCompartmentBuoyancyCenter(p, d), Vector3.up * buoyancy * scale);
        }

        private void OnDrawGizmosSelected()
        {
            var scale = 100.0f;
            var velocity = GetComponent<Rigidbody>().velocity;
            var angularVelocity = GetComponent<Rigidbody>().angularVelocity;

            for (int i = 0; i < compartmentCount; i++) {
                var p = GetCompartmentPosition(compartments[i]);
                var d = GetCompartmenDepth(p);
                var b = GetCompartmentBuoyancy(d, flood[i]);
                var v = GetCompartmentVelocity(velocity, angularVelocity, transform.position, p);
                var drag = GetCompartmentDrag(v, d);

                Gizmos.color = Color.red;
                DrawGravityGizmo(p, d, scale * 0.25f);
                Gizmos.color = Color.green;
                DrawBuoyancyGizmo(p, d, b, scale);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(GetCompartmentBuoyancyCenter(p, d), drag * scale);
            }

            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, extents * 2.0f);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
