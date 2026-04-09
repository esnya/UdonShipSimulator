using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(RoundGauge))]
    public class RoundGaugeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.sourceBehaviour)));
            UssEditorUtility.StringPopupOrField(
                serializedObject.FindProperty(nameof(RoundGauge.variableName)),
                new GUIContent("Variable Name"),
                UssEditorUtility.ListPublicVariableNames(((RoundGauge)target).sourceBehaviour, typeof(float))
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.indicator)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.axis)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.maxAngle)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.maxValue)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RoundGauge.absolute)));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
