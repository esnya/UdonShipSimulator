using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using UnityEngine.Assertions.Must;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Anchor : UdonSharpBehaviour
    {
        /// <summary>
        /// Maximum length of chain from hawspiper in meters.
        /// </summary>
        public float maxChainLength = 25.0f * 16.0f;

        /// <summary>
        /// Diameter of rings in meters.
        /// </summary>
        public float ringDiameter = 0.04f;

        /// <summary>
        /// Position of hawsepiper. Use self if null.
        /// </summary>
        [CanBeNull] public Transform hawsepiper;

        /// <summary>
        /// Length extended from hawsepiper in meters.
        /// </summary>
        public float extendedLength = 0.0f;


        /// <summary>
        /// LayerMask to detect seabed.
        /// </summary>
        public LayerMask seabedLayerMask = 1 | 1 << 11;

        /// <summary>
        /// Approximate size of anchor head in meter.
        /// </summary>
        public float headSize = 1.0f;

        /// <summary>
        /// Anchor holding power in N/kg.
        /// </summary>
        public float holdingPower = 9.80665f * 4.9f;

        private bool anchored;
        private float massPerMeter;
        private Rigidbody vesselRigidbody;
        private GameObject vesselGameObject;
        private Vector3 localForce;
        private bool isOwner;
        private Vector3 headVelocity;
        public float headMass = 1900.0f;
        private RaycastHit hit;
        private Vector3 headPosition;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vesselGameObject = vesselRigidbody.gameObject;

            if (!hawsepiper) hawsepiper = transform;

            massPerMeter = Mathf.Pow(ringDiameter, 2.0f) * 208.0f;
        }

        private void FixedUpdate()
        {
            if (isOwner) Owner_FixedUpdate(Time.fixedDeltaTime);
        }

        private void Owner_FixedUpdate(float deltaTime)
        {
            if (!anchored) return;

            vesselRigidbody.AddForceAtPosition(hawsepiper.TransformVector(localForce), hawsepiper.position);
        }

        private void Update()
        {
            if (isOwner = Networking.IsOwner(vesselGameObject)) Owner_Update(Time.deltaTime);
        }

        private void Owner_Update(float deltaTime)
        {
            if (!anchored)
            {
                localForce = Vector3.zero;

                if (Mathf.Approximately(extendedLength, 0.0f)) return;

                if (Physics.SphereCast(headPosition + Vector3.up * 0.5f, headSize * 0.5f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    headPosition = hit.point;
                    anchored = true;
                }
                else
                {
                    headVelocity = Vector3.zero;
                    headPosition = hawsepiper.position + Vector3.down * extendedLength;
                }
            }
            else
            {
                var hawsepiperPosition = hawsepiper.position;
                var v = headPosition - hawsepiperPosition;
                var distance = v.magnitude;
                var direction = v.normalized;
                var localDirection = hawsepiper.InverseTransformDirection(direction);
                var maxHoldingForce = holdingPower * headMass;

                if (distance > extendedLength)
                {
                    if (Mathf.Abs(v.y) > extendedLength)
                    {
                        anchored = false;
                    }
                    else
                    {
                        var hawspiperXZPosition = Vector3.ProjectOnPlane(hawsepiperPosition, Vector3.up);
                        var headXZPosition = Vector3.ProjectOnPlane(headPosition, Vector3.up);
                        localForce = localDirection * maxHoldingForce;
                        headPosition = (headXZPosition - hawspiperXZPosition) * (extendedLength / distance) + hawspiperXZPosition + Vector3.up * headPosition.y;
                    }
                }
                else
                {
                    var catenaryTension = GetCatenaryT(GetCatenaryD(extendedLength, distance), distance, massPerMeter);
                    localForce = localDirection * Mathf.Clamp(catenaryTension, 0, maxHoldingForce);

                    if (catenaryTension > maxHoldingForce)
                    {
                        headPosition += headVelocity * deltaTime;
                        headVelocity -= direction * (catenaryTension - maxHoldingForce) * (deltaTime / headMass);
                    }
                }
            }
        }

        private float GetCatenaryD(float l, float s)
        {
            var d = l - s;
            return d <= 0.0f ? 0.0f : Mathf.Sqrt(d * s * 3.0f / 8.0f);
        }

        private float GetCatenaryT(float d, float s, float w)
        {
            return w * Mathf.Pow(s, 2.0f) / (d * 8.0f);
        }

        public void _USS_Respawned()
        {
            localForce = headVelocity = Vector3.zero;
            extendedLength = 0.0f;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            var hawsepiperTransform = hawsepiper ?? transform;
            var hawsepiperPosition = hawsepiperTransform.position;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(hawsepiperPosition, headPosition);

            Gizmos.color = anchored ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(headPosition, headSize * 0.5f);

            if (EditorApplication.isPlaying && anchored)
            {
                var forceScale = SceneView.currentDrawingSceneView.size / (vesselRigidbody ?? GetComponentInParent<Rigidbody>()).mass;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(hawsepiperPosition, hawsepiperTransform.TransformVector(localForce) * forceScale);

                var v = headPosition - hawsepiperPosition;
                var d = GetCatenaryD(extendedLength, v.magnitude);
                var p = (hawsepiperPosition + headPosition) * 0.5f + Vector3.Cross(v, Vector3.Cross(v, Vector3.up)).normalized * d;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(hawsepiperPosition, p);
                Gizmos.DrawLine(p, headPosition);
            }
        }
#endif
    }
}
