
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
        /// Resistance factor.
        /// </summary>
        [Range(1.3f, 2.0f)] public float appendageResistanceFactor = 1.5f;

        /// <summary>
        /// Slip stream factor.
        /// </summary>
        [Range(1.2f, 1.5f)] public float k = 1.5f;

        [NonSerialized] public float surfaceArea;
        private float forceMultiplier;
        private Rigidbody vesselRigidbody;
        private float rho = Ocean.OceanRho;
        private float seaLevel;
        private Vector3 localForce;
        private ScrewPropeller[] propellers;
        private Shaft[] propellerShafts;

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
            propellerShafts = new Shaft[propellers.Length];
            for (var i = 0; i < propellers.Length; i++)
            {
                propellerShafts[i] = propellers[i].shaft;
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

            var u = GetVelocity2D();
            var ur = u + GetTotalDeltaUS(u);
            localForce = Vector3.left * (Mathf.Pow(ur.magnitude, 2.0f) * Vector3.Dot(transform.right, ur.normalized) * forceMultiplier);
        }

        private Vector3 GetVelocity2D()
        {
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var centerOfLift = transform.position;
            var velocity = vesselRigidbody.velocity + Vector3.Cross(vesselRigidbody.angularVelocity, centerOfLift - centerOfMass);
            return Vector3.ProjectOnPlane(velocity, transform.up);
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

        private Vector3 GetPropellerDeltaUS(Vector3 u, ScrewPropeller propeller, Shaft shaft)
        {
            var n = shaft.n;
            var direction = propeller.transform.forward;
            var up = Vector3.Dot(u, direction);
            var p = propeller.pitch;
            var du = Mathf.Max(Mathf.Sign(n) * (Mathf.Pow(Mathf.Abs(up), 1.0f - 0.5f * k) * Mathf.Pow(Mathf.Abs(n) * p, 0.5f * k) - up), 0.0f);
            return direction * du;
        }

        private Vector3 GetTotalDeltaUS(Vector3 u)
        {
            var result = Vector3.zero;
            for (var i = 0; i < propellers.Length; i++)
            {
                result += GetPropellerDeltaUS(u, propellers[i], propellerShafts[i]);
            }
            return result;
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

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.0f, depth, length));

                var u = GetVelocity2D();
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(Vector3.zero, transform.InverseTransformVector(u));

                if (propellers != null)
                {
                    var ur = u + GetTotalDeltaUS(u);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(Vector3.zero, transform.InverseTransformVector(ur));

                    Gizmos.color = Color.cyan;
                    foreach (var propeller in propellers)
                    {
                        var shaft = propeller.shaft;
                        if (shaft == null) continue;
                        var dur = GetPropellerDeltaUS(u, propeller, shaft);
                        Gizmos.DrawRay(Vector3.zero, transform.InverseTransformVector(dur));

                        var p = propeller.pitch;
                        var direction = propeller.transform.forward;
                        var up = Vector3.Dot(u, direction);
                        var n = shaft.n;
                        var du = Mathf.Pow(Mathf.Abs(up), 1.0f - 0.5f * k) * Mathf.Pow(Mathf.Abs(n) * p, 0.5f * k) - up;

                        Handles.Label(transform.InverseTransformPoint(propeller.transform.position), $"UP:\t{up:F2}m/s\nnP:\t{n * p:F2}\nUS/UP:\t{du:F4}\nΔUR:\t{dur.magnitude:F2}m/s");
                    }
                }

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
