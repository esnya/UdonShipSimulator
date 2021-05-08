
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Rigidbody))]
    public class UdonShip : UdonSharpBehaviour
    {
        public float waterDensity = 0.99997495f, waterViscosity = 0.000890f;
        public Transform centerOfMass;
        public float waveDrag = 0.5f, frictionDrag = 0.5f;
        Vector3 dragCoefficient = new Vector3(2.0f, 1.5f, 0.5f);
        public Transform rudder;
        public float rudderCoefficient = 0.0002f;
        public float rudderBackwardCoefficient = 0.0001f;
        [ListView("Thrusters/Screws")] public Transform[] thrusters = {};
        [ListView("Thrusters/Screws")] public float[] thrustForces = { 0.0005f };
        [HideInInspector] public float[] thrustPowers;
        public Vector3 extents;
        public TextMeshPro ownerText;

        private float volume;
        private new Rigidbody rigidbody;
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

            thrusterCount = Mathf.Min(thrusters.Length, thrustForces.Length);
            thrustPowers = new float[thrusterCount];

            volume = extents.x * 2 * extents.y * 2 * extents.z * 2;

            OnOwnershipTransferred();

            dead = false;
        }

        private Vector3 GetWorldCenterOfBuoyancy(float draft)
        {
            return Vector3.Scale(Vector3.Scale(transform.up, Vector3.down), extents / extents.y) * draft * 0.5f;
        }

        private float GetDraft()
        {
            var bottomHeight = transform.position.y - extents.y;
            var seaHeight = 0.0f; // waterSurface.position.y;
            return Mathf.Clamp(seaHeight - bottomHeight, 0, extents.y * 2);
        }

        private float GetUnderWaterVolume(float draft)
        {
            return volume * draft / (extents.y * 2);
        }

        private float GetBuoyancy(float volume)
        {
            return -waterDensity * volume * Physics.gravity.y;
        }

        private Vector3 GetDragForce(float draft, Vector3 velocity)
        {
            var localVelocity = transform.InverseTransformVector(velocity);
            var sqrLocalVelocity = Vector3.Scale(localVelocity, localVelocity);

            var bottomArea = extents.x * 2 * extents.z * 2;
            var leftArea = extents.z * 2 * draft;
            var frontArea = extents.x * 2 * draft;
            var surfaceArea = bottomArea + (leftArea + frontArea) * 2.0f;

            var cf = frictionDrag * Vector3.Scale(localVelocity, new Vector3(frontArea * 2 + bottomArea, (frontArea + leftArea) * 2, leftArea * 2 + bottomArea));
            var cw = waveDrag * waterDensity * surfaceArea * sqrLocalVelocity * 0.5f;

            return -transform.TransformVector(cf + cw);
        }

        private void ApplySectionForce(Vector3 section, float volume, Vector3 gravity, float gravityMagnitude, Vector3 localVelocity, Vector3 sqrLocalVelocity)
        {
            var center = transform.TransformPoint(Vector3.Scale(section, extents) * 0.5f);

            var draft = Mathf.Clamp(-center.y, 0, extents.y * 2.0f);
            var underWaterVolume = volume * draft / extents.y * 2.0f;
            var bouyancy = -waterDensity * underWaterVolume * gravity;

            var size = Vector3.Scale(extents * 2.0f, new Vector3(0.5f, 1.0f, 0.25f));
            var sideArea = size.y * size.z * (section.x == Mathf.Sign(localVelocity.x) ? 1.0f : 0.0f);
            var bottomArea = size.x * size.z;
            var frontArea = size.x * size.y * (section.z == 0.0f || section.z == 1.0f ? 1.0f : 0.0f);
            var surfaceArea = sideArea + bottomArea + frontArea;
            var localFrictionForce = transform.TransformVector(-frictionDrag * surfaceArea * localVelocity * gravityMagnitude);

            var localWaveForce = -waveDrag * waterDensity * surfaceArea * sqrLocalVelocity * 0.5f;

            var localResistanceForce = -0.5f * waterDensity * Vector3.Scale(Vector3.Scale(sqrLocalVelocity, new Vector3(sideArea, bottomArea, frontArea)), dragCoefficient);

            rigidbody.AddForceAtPosition(bouyancy + transform.TransformVector(localFrictionForce + localWaveForce + localResistanceForce), center, ForceMode.Force);
        }

        float flood = 0.0f;
        private void FixedUpdate()
        {
            if (!Networking.IsOwner(gameObject)) return;

            flood = dead ? Mathf.Clamp01(flood + Time.fixedDeltaTime * 0.1f) : 0.0f;

            var p0 = transform.position - transform.up * extents.y;

            var p1 = p0 + transform.right * extents.x * 0.5f;
            var p2 = p0 - transform.right * extents.x * 0.5f;
            var p3 = p0 + transform.forward * extents.z * 0.5f;
            var p4 = p0 - transform.forward * extents.z * 0.5f;

            var y0 = 0; // waterSurface.position.y;
            var velocity = rigidbody.velocity;

            var d1 = Mathf.Clamp(y0 - p1.y, 0, extents.y * 2.0f);
            var d2 = Mathf.Clamp(y0 - p2.y, 0, extents.y * 2.0f);
            var d3 = Mathf.Clamp(y0 - p3.y, 0, extents.y * 2.0f);
            var d4 = Mathf.Clamp(y0 - p4.y, 0, extents.y * 2.0f);

            var v1 = volume * 0.25f * d1 / extents.y;
            var v2 = volume * 0.25f * d2 / extents.y;
            var v3 = volume * 0.25f * d3 / extents.y;
            var v4 = volume * 0.25f * d4 / extents.y;

            var b1 = v1 * waterDensity * (1.0f - flood * flood);
            var b2 = v2 * waterDensity * (1.0f - flood * flood);
            var b3 = v3 * waterDensity * (1.0f - flood);
            var b4 = v4 * waterDensity * (1.0f - flood);

            //rigidbody.AddForce(Vector3.up * GetBuoyancy(GetUnderWaterVolume(draft)), ForceMode.Force);
            rigidbody.AddForceAtPosition(Vector3.up * b1, p1 + transform.up * d1 * 0.5f, ForceMode.Force);
            rigidbody.AddForceAtPosition(Vector3.up * b2, p2 + transform.up * d1 * 0.5f, ForceMode.Force);
            rigidbody.AddForceAtPosition(Vector3.up * b3, p3 + transform.up * d1 * 0.5f, ForceMode.Force);
            rigidbody.AddForceAtPosition(Vector3.up * b4, p4 + transform.up * d1 * 0.5f, ForceMode.Force);

            for (int i = 0; i < thrusterCount; i++)
            {
                var thruster = thrusters[i];
                if (thruster.position.y >= y0) continue;

                var power = thrustForces[i] * thrustPowers[i];
                rigidbody.AddForceAtPosition(thruster.forward * power, thruster.position, ForceMode.Force);
            }

            var localVelocity = transform.InverseTransformVector(rigidbody.velocity);
            var cl = Vector3.Dot(velocity.normalized, rudder.right);
            var l = -0.5f * waterDensity * rigidbody.velocity.sqrMagnitude * (localVelocity.z >= 0 ? rudderCoefficient : rudderBackwardCoefficient) * cl;
            rigidbody.AddForceAtPosition(rudder.right * l, rudder.position, ForceMode.Force);

            //rigidbody.AddForce(GetDragForce(Mathf.Clamp(y0 - p0.y, 0, extents.y * 2.0f), velocity));
        }

        public override void OnOwnershipTransferred()
        {
            if (ownerText != null) ownerText.text = Networking.GetOwner(gameObject).displayName;
        }

        public void SetThrustPower(float power)
        {
            for (int i = 0; i < thrusterCount; i++) thrustPowers[i] = Mathf.Clamp(power, -1.0f, 1.0f);
        }

        public void Respawn()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            rigidbody.ResetInertiaTensor();
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            dead = false;
            OnOwnershipTransferred();
        }

        private bool dead;
        public void BulletHit()
        {
            dead = true;
        }

        public override void OnPickup()
        {
            dead = false;
            OnOwnershipTransferred();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
/*
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, extents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void OnDrawGizmosSelected()
        {
            Start();

            var buoyancy = Vector3.up * GetBuoyancy(GetUnderWaterVolume(GetDraft()));
            var gravityForce = rigidbody.mass * Physics.gravity;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetWorldCenterOfBuoyancy(GetDraft()), 0.001f);
            Gizmos.DrawRay(GetWorldCenterOfBuoyancy(GetDraft()), buoyancy);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rigidbody.worldCenterOfMass, 0.001f);
            Gizmos.DrawRay(rigidbody.worldCenterOfMass, gravityForce);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, buoyancy + gravityForce);
        }
        */
#endif
    }
}
