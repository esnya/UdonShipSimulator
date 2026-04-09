using System;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    public static class DebugVisualizationSliceB
    {
        private const GizmoType SelectedGizmo = GizmoType.Selected | GizmoType.Active;

        [DrawGizmo(SelectedGizmo)]
        private static void DrawFlowGizmos(Flow flow, GizmoType gizmoType)
        {
            using (new GizmoMatrixScope(flow.transform.localToWorldMatrix))
            {
                Gizmos.color = new Color(0.2f, 0.6f, 1.0f, 1.0f);
                Gizmos.DrawWireSphere(Vector3.zero, 1.0f);
                DrawArrow(Vector3.zero, Vector3.forward * flow.speed, Gizmos.color);
            }

            Handles.Label(flow.transform.position, $"Flow {flow.speed:F1} m/s");
        }

        [DrawGizmo(SelectedGizmo)]
        private static void DrawBilgeKeelGizmos(BilgeKeel bilgeKeel, GizmoType gizmoType)
        {
            using (new GizmoMatrixScope(bilgeKeel.transform.localToWorldMatrix))
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.right * bilgeKeel.breadth + Vector3.forward * bilgeKeel.length);

                Gizmos.color = new Color(0.3f, 0.8f, 1.0f, 1.0f);
                DrawArrow(Vector3.zero, Vector3.up * Mathf.Max(bilgeKeel.breadth, 0.5f), Gizmos.color);
            }

            Handles.Label(
                bilgeKeel.transform.position,
                $"{(bilgeKeel.isSkeg ? "Skeg" : "Bilge Keel")} {bilgeKeel.breadth:F1} x {bilgeKeel.length:F1} m");
        }

        [DrawGizmo(SelectedGizmo)]
        private static void DrawHullAppendageGizmos(HullAppendage appendage, GizmoType gizmoType)
        {
            using (new GizmoMatrixScope(appendage.transform.localToWorldMatrix))
            {
                Gizmos.color = new Color(1.0f, 0.4f, 0.3f, 1.0f);
                if (appendage.IsCylinder())
                {
                    var radius = appendage.size.x * 0.5f;
                    var halfLength = appendage.size.z * 0.5f;
                    Handles.color = Gizmos.color;
                    Handles.DrawWireDisc(Vector3.forward * halfLength, Vector3.forward, radius);
                    Handles.DrawWireDisc(Vector3.back * halfLength, Vector3.forward, radius);
                    Gizmos.DrawLine(new Vector3(radius, 0.0f, halfLength), new Vector3(radius, 0.0f, -halfLength));
                    Gizmos.DrawLine(new Vector3(-radius, 0.0f, halfLength), new Vector3(-radius, 0.0f, -halfLength));
                    Gizmos.DrawLine(new Vector3(0.0f, radius, halfLength), new Vector3(0.0f, radius, -halfLength));
                    Gizmos.DrawLine(new Vector3(0.0f, -radius, halfLength), new Vector3(0.0f, -radius, -halfLength));
                }
                else
                {
                    Gizmos.DrawWireCube(Vector3.zero, appendage.size);
                }
            }

            Handles.Label(
                appendage.transform.position,
                $"Appendage area {appendage.GetSurfaceArea():F1}, k {appendage.GetMinAppendageResisstanceFactor():F1}-{appendage.GetMaxAppendageResisstanceFactor():F1}");
        }

        [DrawGizmo(SelectedGizmo)]
        private static void DrawWalkingStationGizmos(WalkingStation station, GizmoType gizmoType)
        {
            var origin = station.transform.position;
            var stepTop = origin + Vector3.up * station.stepHeight;
            var headTop = origin + Vector3.up * station.playerHeight;
            var forward = Vector3.ProjectOnPlane(station.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 1.0e-6f)
            {
                forward = Vector3.forward;
            }

            Gizmos.color = new Color(0.2f, 1.0f, 0.4f, 1.0f);
            DrawCapsule(origin, station.playerRadius, station.playerHeight);

            Gizmos.color = new Color(1.0f, 0.9f, 0.2f, 1.0f);
            Gizmos.DrawLine(origin, origin + Vector3.down * station.maxHeight);
            Gizmos.DrawWireSphere(stepTop, station.playerRadius);
            Gizmos.DrawWireSphere(headTop - Vector3.up * station.playerRadius, station.playerRadius);

            var projectedOrigin = origin + forward * station.walkSpeed;
            Gizmos.color = new Color(0.3f, 0.7f, 1.0f, 1.0f);
            DrawCapsule(projectedOrigin, station.playerRadius, station.playerHeight);
            DrawArrow(origin, forward * station.walkSpeed, Gizmos.color);

            Handles.Label(
                origin + Vector3.up * (station.playerHeight + 0.2f),
                $"Walk {station.walkSpeed:F1} m/s, step {station.stepHeight:F1}, max {station.maxHeight:F1}");
        }

        [DrawGizmo(SelectedGizmo)]
        private static void DrawRopeRendererGizmos(RopeRenderer rope, GizmoType gizmoType)
        {
            var controlPoints = rope.controlPoints;
            if (controlPoints == null || controlPoints.Length == 0)
            {
                return;
            }

            for (var index = 0; index < controlPoints.Length; index++)
            {
                var end = controlPoints[index];
                if (!end)
                {
                    continue;
                }

                var start = index == 0 ? rope.transform : controlPoints[index - 1];
                if (!start)
                {
                    continue;
                }

                var segmentLength = GetSegmentLength(rope, index, start.position, end.position);
                var points = BuildSegmentPreview(start.position, end.position, segmentLength, rope.pointPerSegment);

                Handles.color = new Color(0.9f, 0.8f, 0.2f, 1.0f);
                Handles.DrawAAPolyLine(3.0f, points);

                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(start.position, 0.04f);
                Gizmos.DrawWireSphere(end.position, 0.04f);

                var midpoint = points[points.Length / 2];
                Handles.Label(midpoint, $"L{index}: {segmentLength:F1}m");
            }
        }

        private static float GetSegmentLength(RopeRenderer rope, int index, Vector3 start, Vector3 end)
        {
            if (rope.segmentLengthList != null && index < rope.segmentLengthList.Length)
            {
                return Mathf.Max(rope.segmentLengthList[index], 0.0f);
            }

            return Vector3.Distance(start, end);
        }

        private static Vector3[] BuildSegmentPreview(Vector3 start, Vector3 end, float segmentLength, int pointPerSegment)
        {
            var pointCount = Mathf.Max(pointPerSegment, 2);
            var points = new Vector3[pointCount];
            var direction = end - start;
            var span = direction.magnitude;
            var slack = Mathf.Max(segmentLength - span, 0.0f);
            var sag = slack <= 0.0f ? 0.0f : Mathf.Sqrt(slack * Mathf.Max(span, 0.01f) * 3.0f / 8.0f);

            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                var t = pointIndex / (float)(pointCount - 1);
                var point = Vector3.Lerp(start, end, t);
                var weight = Mathf.Sin(t * Mathf.PI);
                points[pointIndex] = point + Vector3.down * (sag * weight);
            }

            return points;
        }

        private static void DrawCapsule(Vector3 origin, float radius, float height)
        {
            var bottom = origin + Vector3.up * radius;
            var top = origin + Vector3.up * Mathf.Max(radius, height - radius);

            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawLine(bottom + Vector3.forward * radius, top + Vector3.forward * radius);
            Gizmos.DrawLine(bottom - Vector3.forward * radius, top - Vector3.forward * radius);
            Gizmos.DrawLine(bottom + Vector3.right * radius, top + Vector3.right * radius);
            Gizmos.DrawLine(bottom - Vector3.right * radius, top - Vector3.right * radius);
        }

        private static void DrawArrow(Vector3 origin, Vector3 vector, Color color)
        {
            if (vector.sqrMagnitude <= 1.0e-6f)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawRay(origin, vector);

            Handles.color = color;
            Handles.ConeHandleCap(
                controlID: 0,
                position: origin + vector,
                rotation: Quaternion.LookRotation(vector.normalized),
                size: HandleUtility.GetHandleSize(origin + vector) * 0.08f,
                eventType: EventType.Repaint);
        }

        private readonly struct GizmoMatrixScope : IDisposable
        {
            private readonly Matrix4x4 previousMatrix;

            public GizmoMatrixScope(Matrix4x4 matrix)
            {
                previousMatrix = Gizmos.matrix;
                Gizmos.matrix = matrix;
            }

            public void Dispose()
            {
                Gizmos.matrix = previousMatrix;
            }
        }
    }
}
