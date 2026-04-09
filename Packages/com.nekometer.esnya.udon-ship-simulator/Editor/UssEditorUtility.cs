using System;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    public static class UssEditorUtility
    {
        public static string[] ListPublicVariableNames(UdonSharpBehaviour behaviour, Type fieldType = null)
        {
            if (behaviour == null)
            {
                return Array.Empty<string>();
            }

            return behaviour.GetType()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null)
                .Where(field => fieldType == null || field.FieldType == fieldType)
                .Select(field => field.Name)
                .ToArray();
        }

        public static string[] ListPublicEventNames(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return Array.Empty<string>();
            }

            return behaviour.GetType()
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Select(method => method.Name)
                .ToArray();
        }

        public static void StringPopupOrField(SerializedProperty property, GUIContent label, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                EditorGUILayout.PropertyField(property, label);
                return;
            }

            var current = property.stringValue ?? string.Empty;
            var currentIndex = Array.IndexOf(options, current);
            var displayOptions = options;
            var displayIndex = currentIndex;

            if (currentIndex < 0)
            {
                var placeholder = string.IsNullOrEmpty(current) ? "(none)" : $"(current) {current}";
                displayOptions = new[] { placeholder }.Concat(options).ToArray();
                displayIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUILayout.Popup(label, displayIndex, displayOptions);
            if (!EditorGUI.EndChangeCheck() || selectedIndex == displayIndex)
            {
                return;
            }

            property.stringValue = currentIndex >= 0
                ? options[selectedIndex]
                : selectedIndex > 0 ? options[selectedIndex - 1] : property.stringValue;
        }

        public static void ResizeArrays(int size, params SerializedProperty[] arrays)
        {
            foreach (var array in arrays)
            {
                array.arraySize = size;
            }
        }
    }
}
