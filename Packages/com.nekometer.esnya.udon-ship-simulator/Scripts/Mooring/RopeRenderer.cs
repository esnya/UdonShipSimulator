
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(LineRenderer))]
    public class RopeRenderer : UdonSharpBehaviour
    {
        /// <summary>
        /// Part of rope in world.
        /// </summary>
        [ListView("Segments")][NotNull][ItemNotNull] public Transform[] controlPoints = { };

        /// <summary>
        /// Rope length to control point.
        /// </summary>
        /// <value></value>
        [ListView("Segments")][Min(0.0f)][NotNull] public float[] segmentLengthList = { };

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
            lineRenderer = GetComponent<LineRenderer>();
            positionOffset = lineRenderer.positionCount;
            lineRenderer.positionCount += controlPoints.Length * pointPerSegment;

            raycastIntervalOffset = UnityEngine.Random.Range(0, raycastInterval);

            groundHeights = new float[controlPoints.Length];
            for (var i = 0; i < groundHeights.Length; i++) groundHeights[i] = float.NaN;
        }

        private void Update()
        {
            if ((Time.renderedFrameCount + raycastIntervalOffset) % raycastInterval == 0)
            {
                var i = UnityEngine.Random.Range(0, controlPoints.Length);
                var cg = (GetControlPointPosition(i - 1) + GetControlPointPosition(i)) / 2.0f;
                groundHeights[i] = Physics.Raycast(cg, Vector3.down, out hit, segmentLengthList[i], groundLeyerMask, queryTriggerInteraction)
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
                var a = Vector3.Cross(i % 2 == 0 ? Vector3.up : -Vector3.down, v).normalized * (segmentLengthList[i] / Mathf.PI);
                var d = groundNotFound ? 0.0f : GetCatenaryD(segmentLengthList[i], v.magnitude);
                var onGround = groundNotFound || Mathf.Approximately(d, 0.0f);

                for (var j = 0; j < pointPerSegment; j++)
                {
                    var t = ((float)j) / (pointPerSegment - 1);
                    var u = Mathf.Sin(t * Mathf.PI);
                    var p = Vector3.Lerp(p1, p2, t);
                    var dj = Mathf.Min(d * u, p.y - h);
                    var w = onGround ? u : Mathf.Clamp01(1.0f - dj / d / u);
                    lineRenderer.SetPosition(positionOffset + i * pointPerSegment + j, transform.InverseTransformPoint(p + w * a + dj * Vector3.down));
                }
            }
        }

        private Vector3 GetControlPointPosition(int i)
        {
            return i == -1 ? transform.position : controlPoints[i].position;
        }

        private float GetCatenaryD(float l, float s)
        {
            var d = l - s;
            return d <= 0.0f ? 0.0f : Mathf.Sqrt(d * s * 3.0f / 8.0f);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!EditorApplication.isPlaying) return;

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
