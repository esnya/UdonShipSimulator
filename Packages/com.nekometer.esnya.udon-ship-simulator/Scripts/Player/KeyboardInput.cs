using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KeyboardInput : UdonSharpBehaviour
    {
        public int[] keyCodes = { };
        public UdonSharpBehaviour[] targets = { };
        public string[] eventNames = { };
        public bool[] controls = { };
        public bool[] shifts = { };
        public bool[] alts = { };

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

    }
}
