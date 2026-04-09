using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    [CustomEditor(typeof(HullSpeedEffect))]
    public class HullSpeedEffectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var particles = serializedObject.FindProperty(nameof(HullSpeedEffect.particles));
            var maxEmissionSpeeds = serializedObject.FindProperty(nameof(HullSpeedEffect.maxEmissionSpeeds));
            var emissionRateCurves = serializedObject.FindProperty(nameof(HullSpeedEffect.emissionRateCurves));
            var keepSeaLevels = serializedObject.FindProperty(nameof(HullSpeedEffect.keepSeaLevels));

            maxEmissionSpeeds.arraySize = particles.arraySize;
            emissionRateCurves.arraySize = particles.arraySize;
            keepSeaLevels.arraySize = particles.arraySize;

            DrawDefaultPropertiesExcept(
                nameof(HullSpeedEffect.particles),
                nameof(HullSpeedEffect.maxEmissionSpeeds),
                nameof(HullSpeedEffect.emissionRateCurves),
                nameof(HullSpeedEffect.keepSeaLevels));
            DrawParticleTable(particles, maxEmissionSpeeds, emissionRateCurves, keepSeaLevels);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawParticleTable(
            SerializedProperty particles,
            SerializedProperty maxEmissionSpeeds,
            SerializedProperty emissionRateCurves,
            SerializedProperty keepSeaLevels)
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
                    maxEmissionSpeeds.arraySize = size;
                    emissionRateCurves.arraySize = size;
                    keepSeaLevels.arraySize = size;
                }

                for (var i = 0; i < particles.arraySize; i++)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.PropertyField(particles.GetArrayElementAtIndex(i), new GUIContent($"Particle {i}"));

                        var maxSpeed = maxEmissionSpeeds.GetArrayElementAtIndex(i);
                        maxSpeed.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField("Max Emission Speed", maxSpeed.floatValue));

                        var curve = emissionRateCurves.GetArrayElementAtIndex(i);
                        curve.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField("Emission Rate Curve", curve.floatValue));

                        EditorGUILayout.PropertyField(keepSeaLevels.GetArrayElementAtIndex(i), new GUIContent("Keep Sea Level"));

                        if (GUILayout.Button("Remove Particle"))
                        {
                            particles.DeleteArrayElementAtIndex(i);
                            maxEmissionSpeeds.DeleteArrayElementAtIndex(i);
                            emissionRateCurves.DeleteArrayElementAtIndex(i);
                            keepSeaLevels.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("Add Particle"))
                {
                    var index = particles.arraySize;
                    particles.arraySize++;
                    maxEmissionSpeeds.arraySize = particles.arraySize;
                    emissionRateCurves.arraySize = particles.arraySize;
                    keepSeaLevels.arraySize = particles.arraySize;
                    maxEmissionSpeeds.GetArrayElementAtIndex(index).floatValue = 0.0f;
                    emissionRateCurves.GetArrayElementAtIndex(index).floatValue = 1.0f;
                    keepSeaLevels.GetArrayElementAtIndex(index).boolValue = false;
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
