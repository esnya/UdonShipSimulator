
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SteamPipe : UdonSharpBehaviour
    {
        /// <summary>
        /// Capacity of hole piple to smoothing pressure.
        /// </summary>
        public float capacity = 10.0f;

        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float steamFlow;
        [NonSerialized] public float steamOutputLimit;
        [NonSerialized] public float steamInputLimit;

        /// <summary>
        /// Total input steam flow in this frame in kg/s.
        /// </summary>
        public float steamInput;
        public float steamOutput;

        private void Start()
        {
            steamFlow = 0;
        }

        private void LateUpdate()
        {
            if (Networking.IsOwner(gameObject))
            {
                var deltaTime = Time.deltaTime;
                var steamOutputLimitTarget = Mathf.Approximately(steamOutput, 0.0f) ? 0.0f : Mathf.Clamp01(steamInput / steamOutput);
                var steamInputLimitTarget = Mathf.Approximately(steamInput, 0.0f) ? 1.0f : Mathf.Clamp01(steamOutput / steamInput);
                steamFlow = steamInput;
                steamInputLimit = Mathf.Lerp(steamInputLimit, steamInputLimitTarget, deltaTime / capacity);
                steamOutputLimit = Mathf.Lerp(steamOutputLimit, steamOutputLimitTarget, deltaTime / capacity);

                steamOutput = 0.0f;
                steamInput = 0.0f;
            }
        }

        public void _USS_Respawned()
        {
            steamFlow = 0;
        }
    }
}
