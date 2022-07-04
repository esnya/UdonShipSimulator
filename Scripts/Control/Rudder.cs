
using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    /// <summary>
    /// Rudder.
    ///
    /// Cites:
    /// [1] 葛湯, 宏彰 : 舵に作用する力と船体・プロペラとの干渉, 日本造船学会誌 第578号 (昭和52)
    /// </summary>
    [DefaultExecutionOrder(100)] // After ScrewPropeller
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Rudder : UdonSharpBehaviour
    {
        /// <summary>
        /// Length in meters.
        /// </summary>
        public float length = 5.0f;

        /// <summary>
        /// Depth in meters.
        /// </summary>
        public float depth = 6.0f;

        /// <summary>
        /// Lift coefficient curve.
        /// </summary>
        public float clMax = 0.8f;

        /// <summary>
        /// Angle of attack with max Cl
        /// </summary>
        public float maxAlpha = 12.0f;

        /// <summary>
        /// Curve of lift.
        /// </summary>
        public float liftCurve = 2.0f;

        /// <summary>
        /// Resistance factor.
        /// </summary>
        [Range(1.3f, 2.0f)] public float appendageResistanceFactor = 1.5f;

        [NonSerialized] public float surfaceArea;
        private float forceMultiplier;
        private Rigidbody vesselRigidbody;
        private float rho = Ocean.OceanRho;
        private float seaLevel;
        private Vector3 localForce;
        private ScrewPropeller[] propellers;
        private float[] usrs;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();


            var ocean = vesselRigidbody.GetComponentInParent<Ocean>();
            if (ocean)
            {
                rho = ocean.rho;
                seaLevel = ocean.transform.position.y;
            }

            propellers = vesselRigidbody.GetComponentsInChildren<ScrewPropeller>(true);
            usrs = new float[propellers.Length];
            for (var i = 0; i < usrs.Length; i++)
            {
                var propeller = propellers[i];
                usrs[i] = Vector3.Distance(propeller.transform.position, transform.position);
            }

            surfaceArea = length * depth;
            forceMultiplier = 0.5f * rho * surfaceArea * GetCfnArFactor();
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

            var localVelocity2D = GetLocalVelocity2D();
            var speed = localVelocity2D.magnitude;
            localForce = Vector3.left * (Mathf.Pow(speed, 2.0f) * localVelocity2D.normalized.x * forceMultiplier);

            // if (float.IsNaN(localForce.x))
            // {
            //     Debug.Log($"{localVelocity2D}, {localForce}, {forceMultiplier}", this);
            // }
        }

        private Vector3 GetLocalVelocity2D()
        {
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var centerOfLift = transform.position;
            var velocity = vesselRigidbody.velocity + Vector3.Cross(vesselRigidbody.angularVelocity, centerOfLift - centerOfMass);
            var localVelocity = transform.InverseTransformDirection(velocity);
            return Vector3.ProjectOnPlane(localVelocity, Vector3.up);
        }

        private float GetAlpha(Vector3 localVelocity2D)
        {
            var angle = Mathf.Abs(Vector3.SignedAngle(Vector3.forward, localVelocity2D, Vector3.up));
            return angle <= 90.0f ? angle : (180.0f - angle);
        }

        private float GetCfnArFactor()
        {
            var ar = depth / length;
            return 6.13f * ar / (ar + 2.25f);
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            try
            {
                this.UpdateProxy();

                if (!vesselRigidbody) vesselRigidbody = GetComponentInParent<Rigidbody>();
                var forceScale = SceneView.currentDrawingSceneView.size * 9.81f / (vesselRigidbody?.mass ?? 1.0f);

                Handles.matrix = Gizmos.matrix = transform.localToWorldMatrix;

                var localVelocity2D = GetLocalVelocity2D();
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(Vector3.zero, localVelocity2D);

                var alpha = GetAlpha(localVelocity2D);
                var alphaAbs = Mathf.Abs(alpha);
                Handles.color = Gizmos.color = alphaAbs <= maxAlpha ? Color.Lerp(Color.white, Color.green, alphaAbs / maxAlpha) : Color.Lerp(Color.green, Color.red, (alphaAbs - maxAlpha) / maxAlpha);
                Handles.Label(Vector3.zero, $"α: {alpha:F2}°");
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.0f, depth, length));

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
