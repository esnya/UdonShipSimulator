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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Anchor : UdonSharpBehaviour
    {
        [Header("Head")]
        /// <summary>
        /// Mass of anchor head in kg.
        /// </summary>
        public float headMass = 1900.0f;

        /// <summary>
        /// Approximate size of anchor head in meter.
        /// </summary>
        public float headSize = 1.0f;

        /// <summary>
        /// Anchor holding power in N/kg.
        /// </summary>
        public float holdingPower = 9.80665f * 4.9f;

        [Header("Chain")]
        /// <summary>
        /// Maximum length of chain from hawspiper in meters.
        /// </summary>
        public float maxChainLength = 25.0f * 16.0f;

        /// <summary>
        /// Diameter of rings in meters.
        /// </summary>
        public float ringDiameter = 0.04f;

        [Header("Others")]
        /// <summary>
        /// LayerMask to detect seabed.
        /// </summary>
        public LayerMask seabedLayerMask = 1 | 1 << 11;
        public int physicsRayCastInterval = 10;

        [Header("Windlass")]

        /// <summary>
        /// Extend or retract speed of windlass in m/s.
        /// </summary>
        public float windlassSpeed = 0.2f;

        [Header("Visuals")]
        /// <summary>
        /// Visual transform of anchor head.
        /// </summary>
        public Transform headVisual;

        /// <summary>
        /// Local up axis of anchor head.
        /// </summary>
        public Vector3 headUpAxis = Vector3.up;

        /// <summary>
        /// Visual transform of anchor shank.
        /// </summary>
        public Transform shankVisual;

        /// <summary>
        /// Local up axis of anchor shank.
        /// </summary>
        public Vector3 shankUpAxis = Vector3.up;

        [Header("Runtime Parameters")]
        /// <summary>
        /// Normalized speed of windlass in -1 to 1.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float windlassTargetSpeed = 0.0f;

        /// <summary>
        /// Length extended from hawsepiper in meters.
        /// </summary>
        [UdonSynced(UdonSyncMode.Smooth)] public float extendedLength = 0.0f;

        private bool anchored;
        private float massPerMeter;
        private Rigidbody vesselRigidbody;
        private Vessel vessel;
        private GameObject vesselGameObject;
        private Vector3 localForce;
        private bool isOwner;
        private Vector3 headVelocity;
        private RaycastHit hit;
        [UdonSynced(UdonSyncMode.Smooth)] private Vector3 headPosition;
        private Vector3 shankInitialPosition;
        private Quaternion shankInitialRotation;
        private LineRenderer debugLineRenderer;
        private float shankLength;
        private float physicsRayCastIntervalOffset;
        private Vector3 headInitialPosition;
        private Quaternion headInitialRotation;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vessel = vesselRigidbody.GetComponent<Vessel>();
            vesselGameObject = vesselRigidbody.gameObject;

            massPerMeter = Mathf.Pow(ringDiameter, 2.0f) * 208.0f;

            if (debugLineRenderer = GetComponentInChildren<LineRenderer>())
            {
                debugLineRenderer.useWorldSpace = true;
            }

            if (shankVisual)
            {
                shankInitialPosition = transform.InverseTransformPoint(shankVisual.position);
                shankInitialRotation = Quaternion.Inverse(transform.rotation) * shankVisual.rotation;
            }

            if (headVisual)
            {
                headInitialPosition = transform.InverseTransformPoint(headVisual.position);
                headInitialRotation = Quaternion.Inverse(transform.rotation) * headVisual.rotation;
            }

            if (shankVisual && headVisual)
            {
                shankLength = Vector3.Distance(shankVisual.position, headVisual.position);
            }

            physicsRayCastIntervalOffset = Random.Range(0.0f, physicsRayCastInterval);

            _USS_Respawned();
        }

        private void FixedUpdate()
        {
            if (isOwner) Owner_FixedUpdate(Time.fixedDeltaTime);
        }

        private void Owner_FixedUpdate(float deltaTime)
        {
            if (!anchored) return;

            vesselRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            if (isOwner = Networking.IsOwner(vesselGameObject)) Owner_Update(Time.deltaTime);

            var position = transform.position;
            var rotation = transform.rotation;
            var v = headPosition - position;
            var rest = extendedLength / headSize;
            var retracted = Mathf.Approximately(extendedLength, 0.0f);
            var pm = retracted ? position : (position + headPosition) * 0.5f + Vector3.Cross(v, Vector3.Cross(v, Vector3.up)).normalized * GetCatenaryD(extendedLength, v.magnitude) * 0.5f;
            var hp = (pm - headPosition).normalized;
            var xzhp = Vector3.ProjectOnPlane(hp, Vector3.up);

            if (shankVisual)
            {
                shankVisual.position = Vector3.Lerp(transform.TransformPoint(shankInitialPosition), headPosition + hp * shankLength, rest);
                shankVisual.rotation = Quaternion.Slerp(rotation * shankInitialRotation, Quaternion.FromToRotation(shankUpAxis, hp), rest);
            }

            if (headVisual)
            {
                headVisual.position = Vector3.Lerp(transform.TransformPoint(headInitialPosition), headPosition, rest);
                headVisual.rotation = Quaternion.Slerp(rotation * headInitialRotation, Quaternion.FromToRotation(headUpAxis, xzhp), rest);
            }

            if (debugLineRenderer)
            {
                if (debugLineRenderer.enabled == retracted)
                {
                    debugLineRenderer.enabled = !retracted;
                    debugLineRenderer.positionCount = retracted ? 0 : 3;
                }

                if (!retracted)
                {
                    debugLineRenderer.SetPosition(0, headPosition + (pm - headPosition).normalized * shankLength);
                    debugLineRenderer.SetPosition(1, pm);
                    debugLineRenderer.SetPosition(2, position);
                }
            }
        }

        private void Owner_Update(float deltaTime)
        {
            if (!Mathf.Approximately(windlassTargetSpeed, 0.0f))
            {
                extendedLength = Mathf.Clamp(extendedLength + windlassSpeed * windlassTargetSpeed * deltaTime, 0.0f, maxChainLength);
            }

            if (!anchored)
            {
                localForce = Vector3.zero;

                if (Mathf.Approximately(extendedLength, 0.0f)) return;

                var xzPosition = Vector3.ProjectOnPlane(headPosition, Vector3.up);
                var seaLevel = vessel.seaLevel;
                var depth = seaLevel - headPosition.y;

                if (depth > headSize && Physics.SphereCast(xzPosition + Vector3.up * (seaLevel - headSize), depth - headSize * 2.0f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    headPosition = hit.point;
                    anchored = true;
                }
                else
                {
                    headVelocity = Vector3.zero;
                    headPosition = transform.position + Vector3.down * extendedLength;
                }
            }
            else
            {
                var hawsepiperPosition = transform.position;
                var v = headPosition - hawsepiperPosition;
                var distance = v.magnitude;
                var direction = v.normalized;
                var localDirection = transform.InverseTransformDirection(direction);
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

                if ((Time.renderedFrameCount + physicsRayCastIntervalOffset) % physicsRayCastIntervalOffset == 0)
                {
                    if (Physics.SphereCast(headPosition + Vector3.up * headSize, headSize * 0.5f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        headPosition = hit.point;
                    }
                    else
                    {
                        anchored = false;
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
            anchored = false;

            if (debugLineRenderer)
            {
                debugLineRenderer.enabled = false;
                debugLineRenderer.positionCount = 0;
            }
        }

        public void Windlass_Stop()
        {
            windlassTargetSpeed = 0.0f;
        }

        public void Windlass_Retract()
        {
            windlassTargetSpeed = -1.0f;
        }

        public void Windlass_Extend()
        {
            windlassTargetSpeed = 1.0f;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            var hawsepiperTransform = transform;
            var hawsepiperPosition = hawsepiperTransform.position;

            if (Mathf.Approximately(extendedLength, 0.0f))
            {
                Gizmos.color = anchored ? Color.green : Color.blue;
                Gizmos.DrawWireSphere(hawsepiperPosition, headSize * 0.5f);
            }
            else
            {
                Gizmos.color = Color.white;
                if (anchored) Gizmos.DrawLine(hawsepiperPosition, headPosition);

                Gizmos.color = anchored ? Color.green : Color.blue;
                Gizmos.DrawWireSphere(headPosition, headSize * 0.5f);
            }

            if (EditorApplication.isPlaying)
            {
                if (anchored)
                {
                    var forceScale = SceneView.currentDrawingSceneView.size / (vesselRigidbody ?? GetComponentInParent<Rigidbody>()).mass;
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(hawsepiperPosition, hawsepiperTransform.TransformVector(localForce) * forceScale);

                    if (!debugLineRenderer)
                    {
                        var v = headPosition - hawsepiperPosition;
                        var d = GetCatenaryD(extendedLength, v.magnitude);
                        var p = (hawsepiperPosition + headPosition) * 0.5f + Vector3.Cross(v, Vector3.Cross(v, Vector3.up)).normalized * d;
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(hawsepiperPosition, p);
                        Gizmos.DrawLine(p, headPosition);
                    }
                }
                else
                {
                    var xzPosition = Vector3.ProjectOnPlane(headPosition, Vector3.up);
                    var seaLevel = vessel.seaLevel;
                    var depth = seaLevel - headPosition.y;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(xzPosition + Vector3.up * (seaLevel - headSize), Vector3.down * (depth - headSize) * 2.0f);
                    if (Physics.SphereCast(xzPosition + Vector3.up * (seaLevel - headSize), depth - headSize * 2.0f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        Gizmos.DrawSphere(hit.point, hit.distance / headSize);
                    }
                }
            }
        }
#endif
    }
}
