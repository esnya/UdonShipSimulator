
using UdonSharp;
using UdonToolkit;
using UnityEngine;

namespace UdonShipSimulator
{
    [
        DefaultExecutionOrder(1000),
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
    ]
    public class VariableWriter : UdonSharpBehaviour
    {
        public bool onStart;
        [ListView("Targets")] public UdonSharpBehaviour[] targets = {};
        [ListView("Targets")] public string[] variableNames = {};
        [ListView("Targets")] public Object[] values = {};

        private int targetCount;
        private void Start()
        {
            targetCount = Mathf.Min(Mathf.Min(targets.Length, variableNames.Length), values.Length);

            if (onStart) Trigger();
        }

        public void Trigger()
        {
            for (int i = 0; i < targetCount; i++)
            {
                targets[i].SetProgramVariable(variableNames[i], values[i]);
            }
        }
    }
}