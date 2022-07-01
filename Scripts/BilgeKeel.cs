using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
#endif

namespace USS2
{
    /// <summary>
    /// Bilge Keel.
    ///
    /// [Ref] Hiroshi Kato: Effects of Bilge Keels on the Rolling of Ships (1965)
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BilgeKeel : UdonSharpBehaviour
    {
        /// <summary>
        /// Beadth in meters.
        /// </summary>
        public float breadth = 1.0f;

        /// <summary>
        /// Length in meters.
        /// </summary>
        public float length = 20.0f;

        [NonSerialized] public int appendageType = HullAppendage.BLIGE_KEEL;
        [NonSerialized] public float appendageResistanceFactor = 1.4f;
        [NonSerialized] public float surfaceArea;
        private Rigidbody vesselRigidbody;
        private Vector3 localForce;
        private float rho = Ocean.OceanRho;
        private float seaLevel;
        private float forceMultiplier;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();

            var ocean = vesselRigidbody.GetComponentInParent<Ocean>();
            if (ocean)
            {
                rho = ocean.rho;
                seaLevel = ocean.transform.position.y;
            }

            surfaceArea = breadth * length;

            var hull = vesselRigidbody.GetComponentInChildren<Hull>();
            var cn = GetCN();
            var ck = hull ? GetCK(hull): 1.0f;
            forceMultiplier = 0.5f * rho * cn * ck * surfaceArea;
        }

        private void FixedUpdate()
        {
            vesselRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            if (transform.position.y > seaLevel)
            {
                localForce = Vector3.zero;
                return;
            }

            var v = GetV();
            localForce = Vector3.down * (Mathf.Pow(v, 2.0f) * Mathf.Sign(v) * forceMultiplier);
            if (float.IsNaN(localForce.x))
            {
                Debug.Log($"{v}, {localForce}, {forceMultiplier}", this);
            }
        }

        private float GetCN()
        {
            return 1.98f * Mathf.Exp(-11.0f * breadth / length);
        }

        private float GetCK(Hull hull)
        {
            var hullLocalPosition = hull.transform.InverseTransformPoint(transform.position);
            var hullLocalCenterOfMass = hull.transform.InverseTransformPoint(vesselRigidbody.worldCenterOfMass);
            var b = hull.beam;
            var fr = (hullLocalPosition.y + hull.depth) / hullLocalPosition.x;
            var kg = hullLocalCenterOfMass.y + hull.depth;
            var r = Vector3.ProjectOnPlane(hullLocalPosition - hullLocalCenterOfMass, hull.transform.forward).magnitude;
            var k = r * Mathf.Pow(1.0f + fr / b, 2.0f) / Mathf.Sqrt(b / 2.0f * kg);
            return 1.0f + 3.5f * Mathf.Exp(-9.0f * k);
        }

        private float GetV()
        {
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var centerOfLift = transform.position;
            var velocity = vesselRigidbody.velocity + Vector3.Cross(vesselRigidbody.angularVelocity, centerOfLift - centerOfMass);
            return Vector3.Dot(velocity, transform.up);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            try
            {
                this.UpdateProxy();

                Handles.matrix = Gizmos.matrix = transform.localToWorldMatrix;

                if (!vesselRigidbody) vesselRigidbody = GetComponentInParent<Rigidbody>();
                var forceScale = SceneView.currentDrawingSceneView.size * 9.81f / (vesselRigidbody?.mass ?? 1.0f);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.right * breadth + Vector3.forward * length);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(Vector3.zero, transform.up * GetV());

                Gizmos.color = Color.green;
                Gizmos.DrawRay(Vector3.zero, localForce * forceScale);
            }
            finally
            {
                Handles.matrix = Gizmos.matrix = Matrix4x4.identity;
            }
        }
#endif
    }
}
