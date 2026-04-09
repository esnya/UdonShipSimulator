using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(HullAppendage))]
    public class HullAppendageEditor : Editor
    {
        private static readonly string[] AppendageTypeOptions =
        {
            "Custom",
            "Rudder behind Skeg",
            "Rudder behind Stern",
            "Twin-Screw Balance Rudders",
            "Shaft Brackets",
            "Skeg",
            "Strut Bossings",
            "Hull Bossings",
            "Shafts",
            "Stabilizer Fins",
            "Dome",
            "Bilge Keel",
        };

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var appendage = (HullAppendage)target;
            var appendageType = serializedObject.FindProperty(nameof(HullAppendage.appendageType));
            var shapeFactor = serializedObject.FindProperty(nameof(HullAppendage.shapeFactor));
            var customShapeFactor = serializedObject.FindProperty(nameof(HullAppendage.customShapeFactor));

            appendageType.intValue = EditorGUILayout.Popup("Appendage Type", Mathf.Clamp(appendageType.intValue, 0, AppendageTypeOptions.Length - 1), AppendageTypeOptions);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HullAppendage.size)));

            if (appendageType.intValue == HullAppendage.CUSTOM)
            {
                EditorGUILayout.PropertyField(customShapeFactor);
            }
            else
            {
                EditorGUILayout.Slider(shapeFactor, 0.0f, 1.0f);
            }

            DrawDefaultPropertiesExcept(
                nameof(HullAppendage.appendageType),
                nameof(HullAppendage.size),
                nameof(HullAppendage.shapeFactor),
                nameof(HullAppendage.customShapeFactor));

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Min Resistance Factor", appendage.GetMinAppendageResisstanceFactor().ToString("F2"));
                EditorGUILayout.LabelField("Max Resistance Factor", appendage.GetMaxAppendageResisstanceFactor().ToString("F2"));
                EditorGUILayout.LabelField("Surface Area", appendage.GetSurfaceArea().ToString("F2"));
                EditorGUILayout.LabelField("Shape", appendage.IsCylinder() ? "Cylinder" : "Box");
            }
        }

        private void DrawDefaultPropertiesExcept(params string[] excludedPropertyNames)
        {
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script" || System.Array.IndexOf(excludedPropertyNames, property.name) >= 0) continue;
                EditorGUILayout.PropertyField(property, true);
            }
        }
    }
}
