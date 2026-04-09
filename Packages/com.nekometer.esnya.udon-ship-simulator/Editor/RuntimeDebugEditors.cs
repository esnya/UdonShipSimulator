using System.Reflection;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    internal static class RuntimeDebugEditors
    {
        private static FieldInfo GetField<T>(string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static readonly FieldInfo AnchorAnchoredField = GetField<Anchor>("anchored");
        private static readonly FieldInfo AnchorHeadPositionField = GetField<Anchor>("headPosition");
        private static readonly FieldInfo AnchorLocalForceField = GetField<Anchor>("localForce");
        private static readonly FieldInfo RudderLocalForceField = GetField<Rudder>("localForce");
        private static readonly FieldInfo ScrewPropellerLocalForceField = GetField<ScrewPropeller>("localForce");

        private static bool TryGetValue<TTarget, TValue>(FieldInfo field, TTarget target, out TValue value)
        {
            if (field != null && target != null)
            {
                var raw = field.GetValue(target);
                if (raw is TValue typed)
                {
                    value = typed;
                    return true;
                }
            }

            value = default;
            return false;
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Selected, typeof(Anchor))]
        private static void DrawAnchorGizmos(Anchor anchor, GizmoType gizmoType)
        {
            if (!anchor) return;

            var hawsepipePosition = anchor.transform.position;
            var extendedLength = anchor.extendedLength;
            var headPosition = hawsepipePosition + Vector3.down * extendedLength;
            var anchored = false;

            TryGetValue(AnchorHeadPositionField, anchor, out headPosition);
            TryGetValue(AnchorAnchoredField, anchor, out anchored);

            Gizmos.color = anchored ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(hawsepipePosition, anchor.headSize * 0.25f);

            if (extendedLength > 0.0f || anchored)
            {
                Gizmos.DrawWireSphere(headPosition, anchor.headSize * 0.5f);

                var span = headPosition - hawsepipePosition;
                var midpoint = (hawsepipePosition + headPosition) * 0.5f;
                var sag = Mathf.Max(anchor.extendedLength - span.magnitude, 0.0f);
                midpoint += Vector3.down * Mathf.Sqrt(Mathf.Max(sag * Mathf.Max(span.magnitude, 0.0f) * 3.0f / 8.0f, 0.0f));

                Gizmos.color = Color.white;
                Gizmos.DrawLine(hawsepipePosition, midpoint);
                Gizmos.DrawLine(midpoint, headPosition);
            }

            if (!EditorApplication.isPlaying) return;
            if (!TryGetValue(AnchorLocalForceField, anchor, out Vector3 localForce)) return;

            var vesselRigidbody = anchor.GetComponentInParent<Rigidbody>();
            var scale = vesselRigidbody ? 1.0f / Mathf.Max(vesselRigidbody.mass, 1.0f) : 1.0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(hawsepipePosition, anchor.transform.TransformVector(localForce) * scale);
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Selected, typeof(Rudder))]
        private static void DrawRudderGizmos(Rudder rudder, GizmoType gizmoType)
        {
            if (!rudder) return;

            try
            {
                Gizmos.matrix = rudder.transform.localToWorldMatrix;
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.01f, rudder.depth, rudder.length));

                if (!EditorApplication.isPlaying) return;
                if (!TryGetValue(RudderLocalForceField, rudder, out Vector3 localForce)) return;

                var vesselRigidbody = rudder.GetComponentInParent<Rigidbody>();
                var scale = vesselRigidbody ? 1.0f / Mathf.Max(vesselRigidbody.mass, 1.0f) : 1.0f;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(Vector3.zero, localForce * scale);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Selected, typeof(ScrewPropeller))]
        private static void DrawScrewPropellerGizmos(ScrewPropeller propeller, GizmoType gizmoType)
        {
            if (!propeller) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(propeller.transform.position, propeller.diameter * 0.5f);

            if (!EditorApplication.isPlaying) return;
            if (!TryGetValue(ScrewPropellerLocalForceField, propeller, out float localForce)) return;

            var vesselRigidbody = propeller.GetComponentInParent<Rigidbody>();
            var scale = vesselRigidbody ? 1.0f / Mathf.Max(vesselRigidbody.mass, 1.0f) : 1.0f;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(propeller.transform.position, propeller.transform.forward * localForce * scale);
        }
    }

    [CustomEditor(typeof(Shaft))]
    public class ShaftEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying) return;

            var shaft = (Shaft)target;
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("RPM", $"{shaft.n * 60.0f:F1}");
                EditorGUILayout.LabelField("Input Torque", $"{shaft.currentInputTorque:F1} N m");
                EditorGUILayout.LabelField("Load Torque", $"{shaft.currentLoadTorque:F1} N m");
                EditorGUILayout.LabelField("Efficiency", $"{shaft.currentEfficiency:F3}");
            }
        }
    }
}
