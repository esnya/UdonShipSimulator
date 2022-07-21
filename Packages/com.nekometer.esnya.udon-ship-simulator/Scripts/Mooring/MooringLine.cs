using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MooringLine : UdonSharpBehaviour
    {
        [Header("Kinematics")]
        /// <summary>
        /// Elasticity per line length in Pa.
        /// </summary>
        [Min(0.0f)] public float elasticity = 2.0E+6f;

        /// <summary>
        /// Maximum load in N.
        // </summary>
        [Min(0.0f)] public float breakingLoad = 60000.0f;

        /// <summary>
        /// Velocity damping.
        /// </summary>
        [Min(0.0f)] public float damping = 1.0f;

        [Header("Dimensions")]
        /// <summary>
        /// Length in meters.
        /// </summary>
        [Min(0.0f)] public float length = 200.0f;

        /// <summary>
        /// Threshold to disable tension in meters.
        /// </summary>
        [Min(0.0f)] public float minLength = 1.0f;

        [Header("References")]
        /// <summary>
        /// Tip of line. Such as MooringLineEye.
        /// </summary>
        [NotNull] public Rigidbody eye;

        /// <summary>
        /// Root of line. GetFromParent if null.
        /// </summary>
        [CanBeNull] public Rigidbody root;

        [Header("Visuals")]
        /// <summary>
        /// Line renderer. Find from children if null.
        /// </summary>
        [CanBeNull] public LineRenderer lineRenderer;

        /// <summary>
        /// Number of catenary curve points to render line.
        /// </summary>
        [Min(2)] public int catenaryPoints = 5;

        /// <summary>
        /// Runtime tension force scale.
        /// </summary>
        [NonSerialized] public float tension;

        private bool isEyeOwner;
        private bool isRootOwner;
        private bool isAnyOwner;
        private bool initialized;
        private float prevDistance;
        private int lineRendererPointOffset;

        public void _USS_VesselStart()
        {
            if (!root)
            {
                root = GetComponentInParent<Rigidbody>();
            }

            if (!lineRenderer) lineRenderer = GetComponentInChildren<LineRenderer>();
            if (lineRenderer)
            {
                lineRendererPointOffset = lineRenderer.positionCount;
                lineRenderer.positionCount += catenaryPoints;
                lineRenderer.SetPosition(lineRendererPointOffset, Vector3.zero);
            }
            eye.gameObject.name = $"{root.gameObject.name}_{gameObject.name}_{eye.gameObject.name}";
            eye.transform.parent = root.transform.parent;

            prevDistance = length;

            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!isAnyOwner) return;

            if (length < minLength)
            {
                tension = 0.0f;
                return;
            }

            var lineVector = eye.position - transform.position;
            var distance = lineVector.magnitude;
            tension = Mathf.Clamp(elasticity * (lineVector.magnitude / length - 1.0f) + (distance - prevDistance) / Time.fixedDeltaTime * damping, -breakingLoad, breakingLoad);
            var force = lineVector.normalized * tension;

            if (isRootOwner) root.AddForceAtPosition(force, transform.position);

            prevDistance = distance;
        }

        private void Update()
        {
            if (!initialized) return;

            isEyeOwner = Networking.IsOwner(eye.gameObject);
            isRootOwner = Networking.IsOwner(root.gameObject);
            isAnyOwner = isEyeOwner || isRootOwner;

            if (lineRenderer)
            {
                var lineRendererTransform = lineRenderer.transform;
                var lineVector = lineRendererTransform.InverseTransformVector(eye.position - transform.position);
                var drop = lineRendererTransform.InverseTransformVector(Vector3.down * GetCatenaryD(length, lineVector.magnitude));

                for (var i = 1; i < catenaryPoints; i++)
                {
                    var t = (float)i / (catenaryPoints - 1);
                    var u = Mathf.Sin(t * Mathf.PI);
                    lineRenderer.SetPosition(i + lineRendererPointOffset, lineVector * t + drop * u);
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
    }
}
