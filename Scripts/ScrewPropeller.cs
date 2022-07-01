
using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScrewPropeller : UdonSharpBehaviour
    {
        /// <summary>
        /// Normalized revolution (Rotation 360 degrees per seconds) by 0-1.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float n = 0.0f;

        [Header("Specs")]

        /// <summary>
        /// Engine or turbine power for the proppeller in Watts.
        /// </summary>
        public float power = 48.0e+6f;

        /// <summary>
        /// Total engine to propeller efficiency.
        /// </summary>
        public float efficiency = 0.5f;

        /// <summary>
        /// Total engine to propeller efficiency reversed.
        /// </summary>
        public float reverseEfficiency = 0.3f;

        [Header("Dimensions")]
        /// <summary>
        /// Propeller diameter in meters.
        /// </summary>
        public float diameter = 0.8f;

        /// <summary>
        /// Shaft length under water in meters.
        /// </summary>
        public float shaftLength = 2.0f;

        /// <summary>
        /// Shaft diameter in meters.
        /// </summary>
        public float shaftDiameter = 0.2f;

        /// <summary>
        /// Shaft resistance form factor.
        /// </summary>
        [Range(2.0f, 4.0f)] public float shaftRegistanceFactor = 2.0f;


        [NonSerialized] public float appendageResistanceFactor;
        [NonSerialized] public float surfaceArea;
        private Rigidbody vesselRigidbody;
        private float localForce;
        private float seaLevel;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();

            var ocean = vesselRigidbody.GetComponentInParent<Ocean>();
            if (ocean)
            {
                seaLevel = ocean.transform.position.y;
            }

            surfaceArea = Mathf.PI * shaftDiameter * shaftLength;
            appendageResistanceFactor = shaftRegistanceFactor;
        }

        private void FixedUpdate()
        {
            vesselRigidbody.AddForceAtPosition(transform.forward * localForce, transform.position);
        }

        private void Update()
        {
            if (transform.position.y > seaLevel)
            {
                localForce = 0.0f;
                return;
            }

            var speed = Vector3.Dot(vesselRigidbody.velocity, transform.forward);
            localForce = (n > 0.0f ? efficiency : reverseEfficiency) * power * n / Mathf.Max(speed, 1.0f);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            var vesselRigidbody = GetComponentInParent<Rigidbody>();

            try {
                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(Vector3.zero, diameter);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(Vector3.zero, shaftDiameter);

                var shaftEnd =  Vector3.forward * shaftLength;
                Gizmos.DrawWireSphere(shaftEnd, shaftDiameter);
                Gizmos.DrawLine(Vector3.zero, shaftEnd);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
                Handles.matrix = Matrix4x4.identity;
            }

            var forceScale = 9.81f / (vesselRigidbody?.mass ?? 1.0f) * 100.0f;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * localForce * forceScale);
        }
#endif
    }
}
