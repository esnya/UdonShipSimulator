using System;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(KeyboardInput))]
    public class KeyboardInputEditor : Editor
    {
        private static readonly string[] KeyCodeNames = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Select(keyCode => keyCode.ToString())
            .ToArray();

        private static readonly int[] KeyCodeValues = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .Select(keyCode => (int)keyCode)
            .ToArray();

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var keyCodes = serializedObject.FindProperty(nameof(KeyboardInput.keyCodes));
            var targets = serializedObject.FindProperty(nameof(KeyboardInput.targets));
            var eventNames = serializedObject.FindProperty(nameof(KeyboardInput.eventNames));
            var controls = serializedObject.FindProperty(nameof(KeyboardInput.controls));
            var shifts = serializedObject.FindProperty(nameof(KeyboardInput.shifts));
            var alts = serializedObject.FindProperty(nameof(KeyboardInput.alts));

            var size = new[] { keyCodes.arraySize, targets.arraySize, eventNames.arraySize, controls.arraySize, shifts.arraySize, alts.arraySize }.Max();
            var newSize = EditorGUILayout.IntField("Binding Count", size);
            if (newSize != size)
            {
                UssEditorUtility.ResizeArrays(newSize, keyCodes, targets, eventNames, controls, shifts, alts);
            }

            for (var i = 0; i < keyCodes.arraySize; i++)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var keyCode = keyCodes.GetArrayElementAtIndex(i);
                    var target = targets.GetArrayElementAtIndex(i);
                    var eventName = eventNames.GetArrayElementAtIndex(i);
                    var control = controls.GetArrayElementAtIndex(i);
                    var shift = shifts.GetArrayElementAtIndex(i);
                    var alt = alts.GetArrayElementAtIndex(i);

                    keyCode.intValue = EditorGUILayout.IntPopup("Key", keyCode.intValue, KeyCodeNames, KeyCodeValues);
                    EditorGUILayout.PropertyField(target, new GUIContent("Target"));
                    UssEditorUtility.StringPopupOrField(
                        eventName,
                        new GUIContent("Event Name"),
                        UssEditorUtility.ListPublicEventNames(target.objectReferenceValue as UdonSharpBehaviour)
                    );
                    EditorGUILayout.PropertyField(control, new GUIContent("Ctrl"));
                    EditorGUILayout.PropertyField(shift, new GUIContent("Shift"));
                    EditorGUILayout.PropertyField(alt, new GUIContent("Alt"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
