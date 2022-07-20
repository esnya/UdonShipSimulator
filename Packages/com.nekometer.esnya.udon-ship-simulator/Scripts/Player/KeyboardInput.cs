using System;
using System.Linq;
using UdonSharp;
using UdonToolkit;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KeyboardInput : UdonSharpBehaviour
    {
        [ListView("Bindings")][Popup("GetKeyCodes")] public int[] keyCodes = { };
        [ListView("Bindings")] public UdonSharpBehaviour[] targets = { };
        [ListView("Bindings")][Popup("behaviour", "@targets")] public string[] eventNames = { };
        [ListView("Bindings")] public bool[] controls = { };
        [ListView("Bindings")] public bool[] shifts = { };
        [ListView("Bindings")] public bool[] alts = { };

        private void Update()
        {
            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            for (var i = 0; i < keyCodes.Length; i++)
            {
                if (Input.GetKeyDown((KeyCode)keyCodes[i]) && controls[i] == ctrl && shifts[i] == shift && alt == alts[i])
                {
                    var target = targets[i];
                    if (target) targets[i].SendCustomEvent(eventNames[i]);
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public string[] GetKeyCodes()
        {
            return Enumerable.Range(0, (Enum.GetValues(typeof(KeyCode)) as int[]).Max()).Select(i => Enum.IsDefined(typeof(KeyCode), i) ? $"{(KeyCode)i}" : string.Empty).ToArray();
        }
#endif
    }
}
