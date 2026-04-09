using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(SteamBoiler))]
    public class SteamBoilerEditor : Editor
    {
        private static readonly string[] ParticleTypeOptions =
        {
            "Smoke",
            "ReriefedSteam",
        };

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var particles = serializedObject.FindProperty(nameof(SteamBoiler.particles));
            var particleTypes = serializedObject.FindProperty(nameof(SteamBoiler.particleTypes));
            particleTypes.arraySize = particles.arraySize;

            DrawDefaultPropertiesExcept(nameof(SteamBoiler.particles), nameof(SteamBoiler.particleTypes));
            DrawParticleTable(particles, particleTypes);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawParticleTable(SerializedProperty particles, SerializedProperty particleTypes)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Particles", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var size = Mathf.Max(0, EditorGUILayout.IntField("Size", particles.arraySize));
                if (EditorGUI.EndChangeCheck())
                {
                    particles.arraySize = size;
                    particleTypes.arraySize = size;
                }

                for (var i = 0; i < particles.arraySize; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(particles.GetArrayElementAtIndex(i), new GUIContent($"Particle {i}"));

                        var type = particleTypes.GetArrayElementAtIndex(i);
                        var index = Mathf.Clamp(type.intValue, 0, ParticleTypeOptions.Length - 1);
                        type.intValue = EditorGUILayout.Popup(index, ParticleTypeOptions, GUILayout.MaxWidth(140));

                        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(24)))
                        {
                            particles.DeleteArrayElementAtIndex(i);
                            particleTypes.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("Add Particle"))
                {
                    var index = particles.arraySize;
                    particles.arraySize++;
                    particleTypes.arraySize = particles.arraySize;
                    particleTypes.GetArrayElementAtIndex(index).intValue = 0;
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
