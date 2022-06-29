
using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScrewPropeller : UdonSharpBehaviour
    {
        /// <summary>
        /// Throttle by 0-1.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float throttle = 0.0f;

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
        /// Shaft blacket area in square.
        /// </summary>
        public float shaftBlacketArea = 5.0f;

        /// <summary>
        /// Area of rudder surface. Readonly.
        /// </summary>
        [NonSerialized] public float surface;
        private Rigidbody vesselRigidbody;
        private Transform vesselTransform;
        private float localThrustForce;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vesselTransform = vesselRigidbody.transform;
        }

        private void FixedUpdate()
        {
            vesselRigidbody.AddForceAtPosition(transform.forward * localThrustForce, transform.position);
        }

        private void Update()
        {
            var speed = Vector3.Dot(vesselRigidbody.velocity, transform.forward);
            localThrustForce = (throttle > 0.0f ? efficiency : reverseEfficiency) * power * throttle / Mathf.Max(speed, 1.0f);
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

            try {
                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(Vector3.zero, diameter);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(Vector3.zero, shaftDiameter);

                var shaftEnd =  Vector3.forward * shaftLength;
                Gizmos.DrawWireSphere(shaftEnd, shaftDiameter);
                Gizmos.DrawLine(Vector3.zero, shaftEnd);

                var bracketSize = Mathf.Sqrt(shaftBlacketArea);
                Gizmos.DrawCube(Vector3.forward * bracketSize * 0.5f, new Vector3(0, bracketSize, bracketSize));
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
                Handles.matrix = Matrix4x4.identity;
            }

            var forceScale = 9.81f / (vesselRigidbody?.mass ?? 1.0f) * 100.0f;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * localThrustForce * forceScale);
        }
#endif
    }
}
