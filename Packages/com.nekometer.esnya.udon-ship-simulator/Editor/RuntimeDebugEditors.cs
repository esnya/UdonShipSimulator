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
        private static readonly FieldInfo RudderPropellersField = GetField<Rudder>("propellers");
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

        private static Vector3 GetRudderVelocity2D(Rudder rudder)
        {
            var vesselRigidbody = rudder.GetComponentInParent<Rigidbody>();
            if (!vesselRigidbody)
            {
                return Vector3.zero;
            }

            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var centerOfLift = rudder.transform.position;
            var velocity = vesselRigidbody.velocity + Vector3.Cross(vesselRigidbody.angularVelocity, centerOfLift - centerOfMass);
            return Vector3.ProjectOnPlane(velocity, rudder.transform.up);
        }

        private static Vector3 GetPropellerDeltaUS(Rudder rudder, Vector3 u, ScrewPropeller propeller, Shaft shaft)
        {
            if (!propeller || !shaft)
            {
                return Vector3.zero;
            }

            var n = shaft.n;
            var direction = propeller.transform.forward;
            var up = Vector3.Dot(u, direction);
            var pitch = propeller.pitch;
            var du = Mathf.Max(Mathf.Sign(n) * (Mathf.Pow(Mathf.Abs(up), 1.0f - 0.5f * rudder.k) * Mathf.Pow(Mathf.Abs(n) * pitch, 0.5f * rudder.k) - up), 0.0f);
            return direction * du;
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

                var u = GetRudderVelocity2D(rudder);
                var propellers = RudderPropellersField?.GetValue(rudder) as ScrewPropeller[];
                var deltaUS = Vector3.zero;
                if (propellers != null)
                {
                    foreach (var propeller in propellers)
                    {
                        var shaft = propeller ? propeller.shaft : null;
                        var dur = GetPropellerDeltaUS(rudder, u, propeller, shaft);
                        deltaUS += dur;
                        if (!propeller || !shaft) continue;

                        var direction = propeller.transform.forward;
                        var up = Vector3.Dot(u, direction);
                        var pitch = propeller.pitch;
                        var n = shaft.n;
                        var du = Mathf.Pow(Mathf.Abs(up), 1.0f - 0.5f * rudder.k) * Mathf.Pow(Mathf.Abs(n) * pitch, 0.5f * rudder.k) - up;
                        Handles.Label(
                            propeller.transform.position,
                            $"UP {up:F2} m/s\nnP {n * pitch:F2}\nUS/UP {du:F3}\nΔUR {dur.magnitude:F2} m/s");
                    }
                }

                Handles.Label(
                    rudder.transform.position,
                    $"|U| {u.magnitude:F2} m/s\n|ΔUS| {deltaUS.magnitude:F2} m/s\n|F| {localForce.magnitude:F1} N");
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

            var shaft = propeller.shaft;
            if (!shaft || !vesselRigidbody) return;

            var axialSpeed = Vector3.Dot(vesselRigidbody.velocity, propeller.transform.forward);
            var absAxialSpeed = Mathf.Abs(axialSpeed);
            var n = shaft.n;
            var absN = Mathf.Abs(n);
            var j = absN > 0.0001f ? propeller.GetJ(absAxialSpeed, n) : 0.0f;
            var torque = propeller.GetPropellerTorque(absAxialSpeed, n);
            var thrust = propeller.GetPropellerThrust(absAxialSpeed, n) * (n < 0 ? propeller.reverseEfficiency : 1.0f);
            var eta0 = absN > 0.0001f ? propeller.GetPropellerEfficiency(j) : 0.0f;

            Handles.Label(
                propeller.transform.position,
                $"N {n * 60.0f:F1} rpm\nQr {torque / 1000.0f:F2} kNm\nT {thrust / 1000.0f:F2} kN\nVa {axialSpeed:F2} m/s\nJ {j:F2}\nη0 {eta0:F2}");
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
