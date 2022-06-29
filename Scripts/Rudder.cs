
using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Rudder : UdonSharpBehaviour
    {
        /// <summary>
        /// Lift coefficient by attack of angle.
        /// </summary>
        [NotNull] public AnimationCurve cl = AnimationCurve.EaseInOut(0.0f, 0.0f, 30.0f, 10.0f);

        /// <summary>
        /// Drag coefficient by attack of angle.
        /// </summary>
        [NotNull] public AnimationCurve cd = AnimationCurve.EaseInOut(0.0f, 0.0f, 90.0f, 10.0f);

        /// <summary>
        /// Rudder surface size
        /// </summary>
        public Vector2 size = new Vector2(4.0f, 5.0f);

        /// <summary>
        /// Rudder max angle
        /// </summary>
        [Range(1.3f, 2.0f)] public float formFactor = 1.5f;

        /// <summary>
        /// Water density. ρ
        /// </summary>
        public float waterDensity = 1025.0f;

        /// <summary>
        /// Area of rudder surface. Readonly.
        /// </summary>
        [NonSerialized] public float surface;
        private float forceMultiplier;
        private Rigidbody vesselRigidbody;
        private Transform vesselTransform;
        private Vector3 localDragForce;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vesselTransform = vesselRigidbody.transform;

            surface = size.x * size.y;
            forceMultiplier = 0.5f * waterDensity * surface;
        }

        private void FixedUpdate()
        {
            vesselRigidbody.AddForceAtPosition(transform.TransformVector(localDragForce), transform.position);
        }

        private void Update()
        {
            localDragForce = GetLocalDragForce();
        }

        private Vector3 GetLocalDragForce()
        {
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var rudderPosition = transform.position;

            var forward = transform.forward;
            var right = transform.right;
            var up = transform.up;

            var velocity = vesselRigidbody.velocity + Vector3.Cross(vesselRigidbody.angularVelocity, rudderPosition - centerOfMass);
            var xzVelocity = Vector3.ProjectOnPlane(velocity, up);


            var speed_2 = xzVelocity.sqrMagnitude;
            var signedAngle = Vector3.SignedAngle(forward, Vector3.ProjectOnPlane(xzVelocity, up), up);
            var angleOfAttack = GetAngleOfAttack(signedAngle);
            var absAoA = Mathf.Abs(angleOfAttack);
            var aoaSign = Mathf.Sign(angleOfAttack);

            return Quaternion.FromToRotation(forward, vesselTransform.forward) * (Vector3.left * cl.Evaluate(absAoA) - Vector3.forward * cd.Evaluate(absAoA)) * aoaSign * speed_2 * forceMultiplier;
        }

        private float GetAngleOfAttack(float signedAngle)
        {
            return signedAngle <= 90.0f ? signedAngle : 180.0f - signedAngle;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            if (!vesselRigidbody)
            {
                vesselRigidbody = GetComponentInParent<Rigidbody>();
                vesselTransform = vesselRigidbody.transform;
            }

            var forceScale = 9.81f / (vesselRigidbody?.mass ?? 1.0f) * 100.0f;
            try {
                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.white;
                Gizmos.DrawCube(Vector3.zero, new Vector3(0.0f, size.y, size.x));
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }

            var worldDragForce = transform.TransformVector(localDragForce);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.Project(worldDragForce, vesselTransform.right) * forceScale);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.Project(worldDragForce, vesselTransform.forward) * forceScale);

            Gizmos.color = Color.white;
        }
#endif
    }
}
