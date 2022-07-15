using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using VRC.Udon;
using UdonSharp;
using System;

namespace USS2
{
    public class RelationGizmos
    {
        private static Dictionary<AbstractUdonProgramSource, Type> programSources;
        private static Dictionary<Type, Texture> icons;

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            var finderPath = AssetDatabase.GetAssetPath(Resources.Load<TextAsset>("USS2")).Split('/');
            var basePath = string.Join("/", finderPath.Take(finderPath.Length - 3));
            programSources = AssetDatabase.FindAssets("t:UdonSharpProgramAsset", new [] { basePath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>)
                .ToDictionary(s => s as AbstractUdonProgramSource, s => s.GetClass());

            icons = new Dictionary<Type, Texture>() {
                { typeof(ScrewPropeller), Resources.Load<Texture>("Icons/Propeller_Outline") },
                { typeof(Rudder), Resources.Load<Texture>("Icons/Rudder_Outline") },
                { typeof(SteamTurbine), Resources.Load<Texture>("Icons/SteamTurbine_Outline") },
                { typeof(SteamPipe), Resources.Load<Texture>("Icons/SteamPipe_Outline") },
                { typeof(Shaft), Resources.Load<Texture>("Icons/Shaft_Outline") },
                { typeof(SteamBoiler), Resources.Load<Texture>("Icons/Boiler_Outline") },
                { typeof(AnalogValueController), Resources.Load<Texture>("Icons/Valvue_Outline") },
            };
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        public static void OnDrawGizmosSelected(UdonBehaviour udon, GizmoType gizmoType)
        {
            if (!programSources.TryGetValue(udon.programSource, out var type)) return;

            var position = udon.transform.position;

            if (icons.TryGetValue(type, out var icon)) Handles.Label(position, icon);

            Gizmos.color = Color.white;
            var relations = udon.publicVariables.VariableSymbols
                .Where(s => udon.publicVariables.TryGetVariableType(s, out var t) && t == typeof(UdonSharpBehaviour))
                .Select(s => udon.GetProgramVariable(s) as UdonSharpBehaviour)
                .Where(b => b != null);
            foreach (var u in relations) Gizmos.DrawLine(position, u.transform.position);
        }
    }
}
