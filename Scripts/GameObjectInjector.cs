
using UdonSharp;
using UdonToolkit;
using UnityEngine;

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(/*BehaviourSyncMode.None*/ BehaviourSyncMode.NoVariableSync)]
    public class GameObjectInjector : UdonSharpBehaviour
    {
        public bool onStart, destroySelf;
        [ListView("Targets")] public Transform[] targets = {};
        [ListView("Targets")] public Transform[] parents = {};
        [ListView("Targets")] public bool[] keepGlobalTransforms = { true };

        private int targetCount;
        private void Start()
        {
            targetCount = Mathf.Min(Mathf.Min(targets.Length, parents.Length), keepGlobalTransforms.Length);

            if (onStart) Trigger();
        }

        public void Trigger()
        {
            for (int i = 0; i < targetCount; i++)
            {
                var target = targets[i];
                var localPosition = target.localPosition;
                var localRotation = target.localRotation;
                var localScale = target.localScale;

                target.parent = parents[i];
                if (!keepGlobalTransforms[i])
                {
                    target.localPosition = localPosition;
                    target.localRotation = localRotation;
                    target.localScale = localScale;
                }
            }
        }
    }
}
