using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using UnityEngine.Assertions.Must;
using System.Linq;

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
        public float windlassSpeed = 1.0f;

        /// <summary>
        /// Extend speed when brake off in m/s.
        /// </summary>
        public float brakeOffSpeed = 3.0f;

        [Header("Visuals")]
        /// <summary>
        /// Visual transform of anchor head.
        /// </summary>
        [CanBeNull] public Transform headVisual;

        /// <summary>
        /// Local up axis of anchor head.
        /// </summary>
        public Vector3 headUpAxis = Vector3.up;

        /// <summary>
        /// Visual transform of anchor shank.
        /// </summary>
        [CanBeNull] public Transform shankVisual;

        /// <summary>
        /// Local up axis of anchor shank.
        /// </summary>
        public Vector3 shankUpAxis = Vector3.up;


        /// <summary>
        /// Line renderer of anchor chain. Get from children if null.
        /// </summary>
        [CanBeNull] public LineRenderer lineRenderer;

        /// <summary>
        /// Name of property overriden.
        /// </summary>
        [NotNull] public string extendedLengthProperty = "_ExtendedLength";

        /// <sumamry>
        /// Texture real length in m.
        /// </summary>
        public float chainTextureRealLength = 0.5f;

        /// <summary>
        /// Name of property overridden.
        /// </summary>
        [NotNull] public string chainTextureRealLengthProperty = "_TextureRealLength";

        [Header("Sounds")]
        /// <sumamry>
        /// Sound of windlass.
        /// </summary>
        [CanBeNull] public AudioSource windlassSound;

        [Header("Runtime Parameters")]
        /// <summary>
        /// Normalized speed of windlass in -1 to 1.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float windlassTargetSpeed = 0.0f;

        /// <summary>
        /// Length extended from hawsepiper in meters.
        /// </summary>
        [Range(0.0f, 1.0f)][UdonSynced(UdonSyncMode.Smooth)] public float windlassBrake = 1.0f;

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
        private float shankLength;
        private float physicsRayCastIntervalOffset;
        private Vector3 headInitialPosition;
        private Quaternion headInitialRotation;
        private float prevExtendedLength;
        private bool _lineRendererEnabled;
        private float initialLineRendererLength;

        private float soundPitchMultiplier;

        private MaterialPropertyBlock properties;
        private int chainDynamicPositionIndex;

        private void Start()
        {
            properties = new MaterialPropertyBlock();

            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vessel = vesselRigidbody.GetComponent<Vessel>();
            vesselGameObject = vesselRigidbody.gameObject;

            massPerMeter = Mathf.Pow(ringDiameter, 2.0f) * 208.0f;

            if (!windlassSound) windlassSound = GetComponentInChildren<AudioSource>();
            if (windlassSound)
            {
                soundPitchMultiplier = windlassSound.pitch / windlassSpeed;
            }

            if (!lineRenderer) lineRenderer = GetComponentInChildren<LineRenderer>();
            if (lineRenderer)
            {
                initialLineRendererLength = 0.0f;
                chainDynamicPositionIndex = lineRenderer.positionCount;
                lineRenderer.positionCount += 3;
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
            if (isOwner) Owner_FixedUpdate();
        }

        private void Owner_FixedUpdate()
        {
            if (!anchored) return;

            vesselRigidbody.AddForceAtPosition(transform.TransformVector(localForce), transform.position);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (isOwner = Networking.IsOwner(vesselGameObject)) Owner_Update(deltaTime);

            var rest = extendedLength / headSize;
            var retracted = Mathf.Approximately(extendedLength, 0.0f);

            var chainMiddlePosition = retracted ? transform.position : GetChainMiddlePosition();
            var chainHeadToMiddle = (chainMiddlePosition - headPosition).normalized;
            var xzhp = Vector3.ProjectOnPlane(chainHeadToMiddle, Vector3.up);

            var windlassWorking = !Mathf.Approximately(extendedLength, prevExtendedLength);
            var windlassCurrentSpeed = (extendedLength - prevExtendedLength) / deltaTime;
            prevExtendedLength = extendedLength;

            if (windlassSound)
            {
                if (windlassWorking)
                {
                    windlassSound.pitch = soundPitchMultiplier * Mathf.Abs(windlassCurrentSpeed);
                }
                if (windlassSound.isPlaying != windlassWorking)
                {
                    if (windlassWorking) windlassSound.Play();
                    else windlassSound.Stop();
                }
            }

            if (shankVisual)
            {
                shankVisual.position = Vector3.Lerp(transform.TransformPoint(shankInitialPosition), headPosition + chainHeadToMiddle * shankLength, rest);
                shankVisual.rotation = Quaternion.Slerp(transform.rotation * shankInitialRotation, Quaternion.FromToRotation(shankUpAxis, chainHeadToMiddle), rest);
            }

            if (headVisual)
            {
                headVisual.position = Vector3.Lerp(transform.TransformPoint(headInitialPosition), headPosition, rest);
                headVisual.rotation = Quaternion.Slerp(transform.rotation * headInitialRotation, Quaternion.FromToRotation(headUpAxis, xzhp), rest);
            }

            if (lineRenderer)
            {
                SetLineRendererEnabled(!retracted);
                if (!retracted)
                {
                    lineRenderer.SetPosition(chainDynamicPositionIndex + 2, lineRenderer.transform.InverseTransformPoint(headPosition + (chainMiddlePosition - headPosition).normalized * shankLength));
                    lineRenderer.SetPosition(chainDynamicPositionIndex + 1, lineRenderer.transform.InverseTransformPoint(chainMiddlePosition));
                    lineRenderer.SetPosition(chainDynamicPositionIndex, lineRenderer.transform.InverseTransformPoint(transform.position));
                }

                if (windlassWorking)
                {
                    lineRenderer.GetPropertyBlock(properties);
                    properties.SetFloat(extendedLengthProperty, extendedLength + initialLineRendererLength);
                    lineRenderer.SetPropertyBlock(properties);
                }
            }
        }

        private void Owner_Update(float deltaTime)
        {
            var extendSpeed = Mathf.Lerp(anchored ? 0.0f : brakeOffSpeed, windlassSpeed * windlassTargetSpeed, windlassBrake);
            if (!Mathf.Approximately(extendSpeed, 0.0f))
            {
                extendedLength = Mathf.Clamp(extendedLength + extendSpeed * deltaTime, 0.0f, maxChainLength);
            }

            if (!anchored)
            {
                localForce = Vector3.zero;

                if (Mathf.Approximately(extendedLength, 0.0f)) return;

                headVelocity = Vector3.zero;
                headPosition = transform.position + Vector3.down * extendedLength;

                if (SphereCast())
                {
                    headPosition = hit.point;
                    anchored = true;
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
                    if (SphereCast())
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

        private bool SphereCast()
        {
            var xzPosition = Vector3.ProjectOnPlane(headPosition, Vector3.up);
            var seaLevel = vessel.seaLevel;
            var depth = seaLevel - headPosition.y;
            return depth > headSize && Physics.SphereCast(xzPosition + Vector3.up * (seaLevel - headSize), depth - headSize * 2.0f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore);
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

        private Vector3 GetChainMiddlePosition()
        {
            return (transform.position + headPosition) * 0.5f + Vector3.down * GetCatenaryD(extendedLength, (headPosition - transform.position).magnitude);
        }

        private void SetLineRendererEnabled(bool value)
        {
            if (!lineRenderer || _lineRendererEnabled == value) return;

            _lineRendererEnabled = value;

            if (!value)
            {
                for (var i = 0; i < 3; i++) lineRenderer.SetPosition(chainDynamicPositionIndex + i, Vector3.zero);
            }
        }

        public void _USS_Respawned()
        {
            localForce = headVelocity = Vector3.zero;
            extendedLength = 0.0f;
            windlassBrake = 1.0f;
            anchored = false;
            SetLineRendererEnabled(false);
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

                    if (!lineRenderer)
                    {
                        var v = headPosition - hawsepiperPosition;
                        var d = GetCatenaryD(extendedLength, v.magnitude);
                        var p = (hawsepiperPosition + headPosition) * 0.5f + Vector3.down * d;
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(hawsepiperPosition, p);
                        Gizmos.DrawLine(p, headPosition);

                        var points = Enumerable.Range(0, 5).Select(i => i / 5.0f).Select(t => v * t + Vector3.down * Mathf.Sin(t * Mathf.PI) * d);
                        foreach (var (p1, p2) in points.Zip(points.Skip(1), (p1, p2) => (p1, p2)))
                        {
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                }
                // else
                // {
                var xzPosition = Vector3.ProjectOnPlane(headPosition, Vector3.up);
                var seaLevel = vessel?.seaLevel ?? 0.0f;
                var depth = seaLevel - headPosition.y;
                Gizmos.color = Color.red;
                // Gizmos.DrawRay(xzPosition + Vector3.up * (seaLevel - headSize), Vector3.down * (depth - headSize) * 2.0f);
                if (Physics.SphereCast(xzPosition + Vector3.up * (seaLevel - headSize), depth - headSize * 2.0f, Vector3.down, out hit, headSize, seabedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Gizmos.DrawSphere(hit.point, hit.distance / headSize);
                }
                // }
            }
        }
#endif
    }
}
