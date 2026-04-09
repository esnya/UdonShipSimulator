using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(AnalogValueController))]
    public class AnalogValueControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.target)));
            UssEditorUtility.StringPopupOrField(
                serializedObject.FindProperty(nameof(AnalogValueController.variableName)),
                new GUIContent("Variable Name"),
                UssEditorUtility.ListPublicVariableNames(((AnalogValueController)target).target, typeof(float))
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_value"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.valueBias)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.valueMultiplier)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.increaseStep)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.fastIncreaseStep)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Transforms", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.visualTransforms)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.rotationScales)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnalogValueController.rotationAxises)), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
