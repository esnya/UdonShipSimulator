using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(LineRenderer))]
    public class RopeRenderer : UdonSharpBehaviour
    {
        /// <summary>
        /// Part of rope in world.
        /// </summary>
        [NotNull][ItemNotNull] public Transform[] controlPoints = { };

        /// <summary>
        /// Rope length to control point.
        /// </summary>
        /// <value></value>
        [Min(0.0f)][NotNull] public float[] segmentLengthList = { };

        /// <summary>
        /// Segments of rope rendering.
        /// </summary>
        public int pointPerSegment = 10;

        /// <summary>
        /// Raycast interval to detect ground.
        /// </summary>
        public int raycastInterval = 10;

        /// <summary>
        /// Ground layer mask for raycast.
        /// </summary>
        public LayerMask groundLeyerMask = -1;

        /// <summary>
        /// Raycast option.
        /// </summary>
        public QueryTriggerInteraction queryTriggerInteraction;

        private LineRenderer lineRenderer;
        private int positionOffset = 0;
        private int raycastIntervalOffset;
        private float[] groundHeights;
        private RaycastHit hit;

        private void Start()
        {
            controlPoints ??= Array.Empty<Transform>();
            segmentLengthList ??= Array.Empty<float>();
            pointPerSegment = Mathf.Max(pointPerSegment, 2);
            raycastInterval = Mathf.Max(raycastInterval, 1);

            lineRenderer = GetComponent<LineRenderer>();
            positionOffset = lineRenderer.positionCount;
            lineRenderer.positionCount += controlPoints.Length * pointPerSegment;

            raycastIntervalOffset = UnityEngine.Random.Range(0, raycastInterval);

            groundHeights = new float[controlPoints.Length];
            for (var i = 0; i < groundHeights.Length; i++) groundHeights[i] = float.NaN;
        }

        private void Update()
        {
            if (controlPoints == null || controlPoints.Length == 0) return;

            if ((Time.renderedFrameCount + raycastIntervalOffset) % raycastInterval == 0)
            {
                var i = UnityEngine.Random.Range(0, controlPoints.Length);
                var cg = (GetControlPointPosition(i - 1) + GetControlPointPosition(i)) / 2.0f;
                groundHeights[i] = Physics.Raycast(cg, Vector3.down, out hit, GetSegmentLength(i), groundLeyerMask, queryTriggerInteraction)
                    ? hit.point.y
                    : float.NaN;
            }

            for (var i = 0; i < controlPoints.Length; i++)
            {
                var p1 = GetControlPointPosition(i - 1);
                var p2 = GetControlPointPosition(i);
                var v = p2 - p1;

                var h = groundHeights[i];
                var groundNotFound = float.IsNaN(h);
                var segmentLength = GetSegmentLength(i);
                var a = Vector3.Cross(i % 2 == 0 ? Vector3.up : -Vector3.down, v).normalized * (segmentLength / Mathf.PI);
                var d = groundNotFound ? 0.0f : GetCatenaryD(segmentLength, v.magnitude);
                var onGround = groundNotFound || Mathf.Approximately(d, 0.0f);

                for (var j = 0; j < pointPerSegment; j++)
                {
                    var t = ((float)j) / (pointPerSegment - 1);
                    var u = Mathf.Sin(t * Mathf.PI);
                    var p = Vector3.Lerp(p1, p2, t);
                    var dj = Mathf.Min(d * u, p.y - h);
                    var w = onGround || Mathf.Approximately(u, 0.0f) ? u : Mathf.Clamp01(1.0f - dj / d / u);
                    lineRenderer.SetPosition(positionOffset + i * pointPerSegment + j, transform.InverseTransformPoint(p + w * a + dj * Vector3.down));
                }
            }
        }

        private Vector3 GetControlPointPosition(int i)
        {
            if (i < 0 || controlPoints == null || i >= controlPoints.Length || controlPoints[i] == null)
            {
                return transform.position;
            }

            return controlPoints[i].position;
        }

        private float GetSegmentLength(int i)
        {
            if (segmentLengthList == null || i < 0 || i >= segmentLengthList.Length)
            {
                return 0.0f;
            }

            return Mathf.Max(segmentLengthList[i], 0.0f);
        }

        private float GetCatenaryD(float l, float s)
        {
            var d = l - s;
            return d <= 0.0f ? 0.0f : Mathf.Sqrt(d * s * 3.0f / 8.0f);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            for (var i = 0; i < controlPoints.Length; i++)
            {
                var p1 = GetControlPointPosition(i - 1);
                var p2 = GetControlPointPosition(i);
                var pc = (p1 + p2) / 2.0f;

                Gizmos.color = float.IsNaN(groundHeights[i]) ? Color.red : Color.green;
                Gizmos.DrawWireSphere(pc, 0.1f);

                if (!float.IsNaN(groundHeights[i]))
                {
                    Gizmos.DrawRay(pc, Vector3.up * (groundHeights[i] - pc.y));
                }
            }
        }
#endif
    }
}
