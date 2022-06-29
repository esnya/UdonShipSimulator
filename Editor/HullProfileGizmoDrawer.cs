using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace USS
{
    /// <summary>
    /// Gizmos for HullProfile.
    /// </summary>
    public class HullProfileGizmoDrawer
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy, typeof(UdonBehaviour))]
        public static void OnDrawGizmosSelected(UdonBehaviour udon, GizmoType gizmoType)
        {
            if (!UdonSharpEditorUtility.IsUdonSharpBehaviour(udon)) return;

            var hullProfile = UdonSharpEditorUtility.GetProxyBehaviour(udon) as HullProfile;
            if (!hullProfile) return;

            hullProfile._UpdateParameters();

            try {
                Gizmos.matrix = Matrix4x4.Rotate(Quaternion.AngleAxis(90.0f, Vector3.forward) * Quaternion.AngleAxis(90.0f, Vector3.up)) * hullProfile.transform.localToWorldMatrix;

                DrawProfile(hullProfile);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        private static void DrawProfile(HullProfile hullProfile)
        {
            var dx = hullProfile.length / hullProfile.curveSamplingCount;
            var keelPoints = Enumerable.Range(0, hullProfile.curveSamplingCount).Select(i => i * dx).Select(x => new Vector3(x, 0, hullProfile.GetKeelDepthAt(x))).ToArray();

            foreach (var (a, b) in keelPoints.Skip(1).Zip(keelPoints, (a, b) => (a, b)))
            {
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
