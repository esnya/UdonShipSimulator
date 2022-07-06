using UdonSharp;
using UnityEngine;
using JetBrains.Annotations;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScrewPropeller : UdonSharpBehaviour
    {

        [Header("Specs")]

        /// <summary>
        /// Thrust coefficient with J = 0.
        /// /// </summary>
        public float tAlpha = 0.4f;

        /// <summary>
        /// J with thrust coefficient = 0.
        /// </summary>
        public float tBeta = 0.8f;

        /// <summary>
        /// Torque coefficient with J = 0.
        /// </summary>
        public float qAlpha = 0.01f;

        /// <summary>
        /// J with torque coefficient = 0.
        /// </summary>
        public float qBeta = 0.9f;

        /// <summary>
        /// Total engine to propeller efficiency reversed.
        /// </summary>
        public float reverseEfficiency = 0.3f;

        /// <summary>
        /// Efficiency by resistance of shafts.
        /// </summary>
        public float shaftEfficiency = 0.99f;


        [Header("Dimensions")]
        /// <summary>
        /// Propeller diameter in meters.
        /// </summary>
        public float diameter = 3.3f;

        /// <summary>
        /// Propeller boss diameter in meters.
        /// </summary>
        public float bossDiameter = 0.2f;

        /// <summary>
        /// Propeller pitch in meters.
        /// </summary>
        public float pitch = 0.3545f;

        /// <summary>
        /// Number of blades
        /// </summary>
        public int blades = 3;

        [Header("Input")]
        [NotNull] public Shaft shaft;

        [Header("Runtime Status")]
        [NotNull] public AnimationCurve etaH = AnimationCurve.Constant(0.0f, 100.0f, 1.0f);
        public float etaR = 0.98f;

        private Rigidbody vesselRigidbody;
        private float localForce;
        private float rho = Ocean.OceanRho;
        private float seaLevel;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();

            var ocean = vesselRigidbody.GetComponentInParent<Ocean>();
            if (ocean)
            {
                rho = ocean.rho;
                seaLevel = ocean.transform.position.y;
            }
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
            var vs = Mathf.Abs(speed);

            var n = shaft.n;
            shaft.loadTorque += GetPropellerTorque(vs, n);
            shaft.efficiency *= GetEfficiency(vs);

            localForce = GetPropellerThrust(vs, n) * (n < 0 ? reverseEfficiency : 1.0f);
        }

        public float GetEfficiency(float v)
        {
            return shaftEfficiency * etaR * (etaH == null ? 1.0f : etaH.Evaluate(Mathf.Abs(v)));
        }

        private float GetForceOrTorque(float va, float n, float a, float b, float d, float rho, float dd)
        {
            return Mathf.Abs(n * d - va / b) * rho * n * Mathf.Pow(d, dd - 1.0f) * a;
        }

        public float GetPropellerThrust(float va, float n)
        {
            return GetForceOrTorque(va, n, tAlpha, tBeta, diameter, rho, 4.0f);
        }

        public float GetPropellerTorque(float va, float n)
        {
            return GetForceOrTorque(va, n, qAlpha, qBeta, diameter, rho, 5.0f);
        }

        public float GetJ(float v, float n)
        {
            return Mathf.Abs(v) / diameter / Mathf.Abs(n);
        }

        public float GetKT(float j)
        {
            return Mathf.Max((1.0f - j / tBeta) * tAlpha, 0.0f);
        }

        public float GetKQ(float j)
        {
            return Mathf.Max((1.0f - j / qBeta) * qAlpha, 0.0f);
        }

        public float GetPropellerEfficiency(float j)
        {
            return j * GetKT(j) / (2.0f * Mathf.PI * GetKQ(j));
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            var vesselRigidbody = GetComponentInParent<Rigidbody>();

            if (EditorApplication.isPlaying)
            {
                var forceScale = SceneView.currentDrawingSceneView.size * 9.81f / (vesselRigidbody?.mass ?? 1.0f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, transform.forward * localForce * forceScale);

                var speed = Vector3.Dot(vesselRigidbody.velocity, transform.forward);
                var vs = speed;

                var n = shaft.n;
                var qr = GetPropellerTorque(vs, n);

                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position, diameter * 0.5f);

                var j = GetJ(vs, n);
                var eta0 = GetPropellerEfficiency(j);

                Handles.Label(
                    transform.position,
                    string.Join("\n", new [] {
                        $"N:\t{n * 60.0f:F2}rpm",
                        $"Qr:\t{qr / 1000.0f:F2}kNm",
                        $"T:\t{GetPropellerThrust(vs, n) / 1000.0f:F2}kN",
                        "",
                        $"Va:\t{vs:F2}m/s",
                        $"J:\t{j:F2}",
                        $"KT:\t{GetKT(j):F2}",
                        $"KQ:\t{GetKQ(j):F2}",
                        $"η0:\t{eta0:F2}",
                        $"η:\t{GetEfficiency(vs) * eta0:F2}",
                    })
                );
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, diameter * 0.5f);
            }
        }
#endif
    }
}
