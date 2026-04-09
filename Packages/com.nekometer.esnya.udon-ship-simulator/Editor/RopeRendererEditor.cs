using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(RopeRenderer))]
    public class RopeRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var controlPoints = serializedObject.FindProperty(nameof(RopeRenderer.controlPoints));
            var segmentLengths = serializedObject.FindProperty(nameof(RopeRenderer.segmentLengthList));
            segmentLengths.arraySize = controlPoints.arraySize;

            DrawDefaultPropertiesExcept(nameof(RopeRenderer.controlPoints), nameof(RopeRenderer.segmentLengthList));
            DrawSegmentsTable(controlPoints, segmentLengths);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSegmentsTable(SerializedProperty controlPoints, SerializedProperty segmentLengths)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var size = Mathf.Max(0, EditorGUILayout.IntField("Size", controlPoints.arraySize));
                if (EditorGUI.EndChangeCheck())
                {
                    controlPoints.arraySize = size;
                    segmentLengths.arraySize = size;
                }

                for (var i = 0; i < controlPoints.arraySize; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(controlPoints.GetArrayElementAtIndex(i), new GUIContent($"Point {i}"));

                        var lengthProperty = segmentLengths.GetArrayElementAtIndex(i);
                        lengthProperty.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(lengthProperty.floatValue, GUILayout.MaxWidth(90)));

                        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(24)))
                        {
                            controlPoints.DeleteArrayElementAtIndex(i);
                            segmentLengths.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("Add Segment"))
                {
                    var index = controlPoints.arraySize;
                    controlPoints.arraySize++;
                    segmentLengths.arraySize = controlPoints.arraySize;
                    segmentLengths.GetArrayElementAtIndex(index).floatValue = 0.0f;
                }
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
