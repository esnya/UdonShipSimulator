using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(ScrewPropeller))]
    public class ScrewPropellerEditor : Editor
    {
        private float rpm;
        private float speed = 30;
        private float throttle = 1;
        private float powerRange;
        private float torqueRange;
        private bool powerBySpeed;
        private bool efficiencyByAdvanceRatio;
        private bool powerByRPM;
        private bool torqueBySpeed;
        private bool torqueByRPM;

        private void OnEnable()
        {
            var propeller = target as ScrewPropeller;
            if (!propeller) return;

            rpm = propeller.maxRPM;
            torqueRange = (propeller.GetComponentInParent<Rigidbody>()?.mass ?? 10000.0f) / propeller.diameter;
            powerRange = propeller.power;
        }

        private static bool IsNaNorInf(float v)
        {
            return float.IsNaN(v) || float.IsInfinity(v);
        }

        private static void DrawGraph(
            Vector2 min,
            Vector2 max,
            Vector2Int grids,
            IEnumerable<(IEnumerable<Vector2>, Color, string)> series,
            string title = null,
            float aspectRatio = 2
        )
        {
            if (!string.IsNullOrEmpty(title))
            {
                EditorGUILayout.LabelField(title);
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var rect = GUILayoutUtility.GetAspectRect(aspectRatio);
                EditorGUILayout.Space();

                var isRepainting = Event.current.type == EventType.Repaint;

                try
                {
                    Handles.color = Color.white;

                    var range = max - min;
                    int xGrid = grids.x;
                    int yGrid = grids.y;

                    if (IsNaNorInf(range.magnitude) || range.x <= 0 || range.y <= 0) return;

                    var scale = new Vector3(rect.width / range.x, -rect.height / range.y, 1);
                    Handles.matrix = Matrix4x4.TRS(new Vector3(rect.x, rect.max.y - min.y * scale.y), Quaternion.identity, scale);

                    if (isRepainting)
                    {
                        for (var x = min.x; x <= max.x; x += range.x / xGrid)
                        {
                            var p1 = new Vector3(x, min.y);
                            var p2 = new Vector3(x, max.y);

                            Handles.color = Color.grey;
                            Handles.DrawLine(p1, p2);

                            Handles.color = Color.white;
                            Handles.Label(new Vector2(x, 0), x.ToString("F1"));
                        }
                        for (var y = min.y; y <= max.y; y += range.y / yGrid)
                        {
                            var p1 = new Vector3(min.x, y);
                            var p2 = new Vector3(max.x, y);

                            Handles.color = Color.grey;
                            Handles.DrawLine(p1, p2);

                            Handles.color = Color.white;
                            Handles.Label(new Vector2(0, y), y.ToString("F1"));
                        }

                        Handles.color = Color.white;
                        Handles.DrawLine(new Vector2(min.x, 0), new Vector2(max.x, 0));
                        Handles.DrawLine(new Vector2(0, min.y), new Vector2(0, max.y));
                    }

                    foreach (var (points, color, name) in series)
                    {
                        if (isRepainting)
                        {
                            Handles.color = color;
                            Handles.DrawPolyLine(points.Where(v => !IsNaNorInf(v.magnitude)).Select(v => (Vector3)v).ToArray());
                        }

                        EditorGUILayout.LabelField(
                            $"━ {name}",
                            new GUIStyle(GUI.skin.label)
                            {
                                normal = new GUIStyleState()
                                {
                                    textColor = color,
                                }
                            }
                        );
                    }
                }
                finally
                {
                    Handles.matrix = Matrix4x4.identity;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var propeller = target as ScrewPropeller;

            var step = 100;
            var jMax = 1.5f;
            var jRange = Enumerable.Range(0, step).Select(i => jMax * i / step).ToArray();
            var ktPoints = jRange.Select(j => new Vector2(j, propeller.GetKT(j))).Where(v => v.y > 0.0f);
            var kqPoints = jRange.Select(j => new Vector2(j, propeller.GetKQ(j))).Where(v => v.y > 0.0f);
            var eta0Points = jRange.Select(j => new Vector2(j, propeller.GetPropellerEfficiency(j))).Where(v => v.y > 0.0f).ToArray();
            var eta0Max = eta0Points.Max(v => v.y);

            if (efficiencyByAdvanceRatio = EditorGUILayout.Foldout(efficiencyByAdvanceRatio, "Propeller Efficiency by Advance Ratio (J)"))
            {
                DrawGraph(
                    Vector2.zero,
                    new Vector2(jMax, Mathf.Ceil(eta0Max / 0.2f) * 0.2f),
                    new Vector2Int(8, 5),
                    new[] {
                        (ktPoints, Color.blue, "KT"),
                        (kqPoints, Color.red, "KQ"),
                        (eta0Points, Color.green, "η0"),
                    }
                );
            }

            var pMax = powerRange;
            var pScale = 1.0f / 1000.0f;
            var nMax = propeller.maxRPM / 60.0f;

            var qScale = 1.0f / 1000.0f;
            var qMax = torqueRange;
            if (torqueBySpeed = EditorGUILayout.Foldout(torqueBySpeed, "Torque [kNm] By Speed [m/s]"))
            {
                var rev = rpm / 60.0f;
                var vMax = 80.0f;
                var vRange = Enumerable.Range(0, step).Select(i => vMax * i / step).ToArray();
                DrawGraph(
                    new Vector2(0, -qMax * qScale),
                    new Vector2(vMax, qMax * qScale),
                    new Vector2Int(4, 4),
                    new[] {
                    (
                        vRange.Select(v => new Vector2(v, propeller.GetPropellerTorque(v, rev) * rev * qScale)),
                        Color.red,
                        "Required Torque"
                    ),
                    (
                        vRange.Select(v => new Vector2(v, propeller.GetAvailableTorque(v, throttle)) * qScale),
                        Color.blue,
                        "Available Torque"
                    ),
                    (
                        vRange.Select(v => new Vector2(v, propeller.GetExcessTorque(v, throttle, rev) * qScale)),
                        Color.green,
                        "Excessed Torque"
                    ),
                    }
                );
                rpm = EditorGUILayout.Slider("RPM", rpm, 0, propeller.maxRPM);
                throttle = EditorGUILayout.Slider("Throttle", throttle, 0.0f, 1.0f);
                torqueRange = Mathf.Max(EditorGUILayout.FloatField("Torque Range", torqueRange), 0.0f);
            }

            if (torqueByRPM = EditorGUILayout.Foldout(torqueByRPM, "Torque [kNm] by RPM"))
            {
                var nRange = Enumerable.Range(0, step).Select(i => nMax * i / step).ToArray();
                DrawGraph(
                    new Vector2(0, -qMax * qScale),
                    new Vector2(nMax * 60.0f, qMax * qScale),
                    new Vector2Int(4, 4),
                    new[] {
                    (
                        nRange.Select(n => new Vector2(n * 60.0f, propeller.GetPropellerTorque(speed, n) * n) * qScale),
                        Color.red,
                        "Required Torque"
                    ),
                    (
                        nRange.Select(n => new Vector2(n * 60.0f, propeller.GetAvailableTorque(speed, throttle)) * qScale),
                        Color.blue,
                        "Available Torque"
                    ),
                    (
                        nRange.Select(n => new Vector2(n * 60.0f, propeller.GetExcessTorque(speed, throttle, n) * qScale)),
                        Color.green,
                        "Excessed Torque"
                    ),
                    }
                );
                speed = EditorGUILayout.Slider("Speed", speed, 0, 100.0f);
                throttle = EditorGUILayout.Slider("Throttle", throttle, 0.0f, 1.0f);
                torqueRange = Mathf.Max(EditorGUILayout.FloatField("Torque Range", torqueRange), 0.0f);
            }
        }
    }
}
