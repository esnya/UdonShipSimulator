using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(HullProfile))]
    public class HullProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var hullProfile = target as HullProfile;

            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                EditorGUILayout.PropertyField(property, true);
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Specs", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("AM (Midship Section Area)", $"{hullProfile.midshipSectionArea:F2}㎡");
                EditorGUILayout.LabelField("AW (Waterplane Area)", $"{hullProfile.waterplaneArea:F2}㎡");
            }
        }
    }
}
