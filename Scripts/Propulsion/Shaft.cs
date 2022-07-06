using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Shaft : UdonSharpBehaviour
    {
        /// <summary>
        /// Resistance of rotation such as flywheel.
        /// </summary>
        [Min(1.0f)] public float momentOfInertia = 1000000.0f;

        [Header("Resistance")]
        /// <summary>
        /// Length under water in meters.
        /// </summary>
        public float length = 10.0f;

        /// <summary>
        /// Diameter under water in meters.
        /// </summary>
        public float diameter = 0.4f;

        /// <summary>
        /// Resistance factor. 2.0 to 4.0.
        /// </summary>
        [Range(2.0f, 4.0f)] public float appendageResistanceFactor = 2.0f;

        [Header("Runtime Parameters")]
        public float n;
        public float inputTorque;
        public float loadTorque;
        public float efficiency = 1.0f;
        [NonSerialized] public float surfaceArea;

        private void Start()
        {
            surfaceArea = Mathf.PI * diameter * length;
        }

        private void LateUpdate()
        {
            n += (inputTorque * efficiency - loadTorque) * Time.deltaTime / momentOfInertia;
            if (float.IsInfinity(n) || float.IsNaN(n)) n = 0.0f;

            inputTorque = 0;
            loadTorque = 0;
            efficiency = 1.0f;
        }

        public void _USS_Respawned()
        {
            n = 0.0f;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            try {
                this.UpdateProxy();

                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(Vector3.forward * length * 0.5f, Vector3.back * length * 0.5f);
                Gizmos.DrawWireSphere(Vector3.zero, diameter * 0.5f);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
#endif
    }
}
