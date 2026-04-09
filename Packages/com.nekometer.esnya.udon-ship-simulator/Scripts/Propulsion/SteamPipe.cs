using System;
using UdonSharp;
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
            steamInputLimit = 1.0f;
            steamOutputLimit = 1.0f;
        }

        private void LateUpdate()
        {
            if (Networking.IsOwner(gameObject))
            {
                var deltaTime = Time.deltaTime;
                var smoothing = capacity <= 0.0f ? 1.0f : deltaTime / capacity;
                var steamOutputLimitTarget = Mathf.Approximately(steamOutput, 0.0f) ? 0.0f : Mathf.Clamp01(steamInput / steamOutput);
                var steamInputLimitTarget = Mathf.Approximately(steamInput, 0.0f) ? 1.0f : Mathf.Clamp01(steamOutput / steamInput);
                steamFlow = steamInput;
                steamInputLimit = Mathf.Lerp(steamInputLimit, steamInputLimitTarget, smoothing);
                steamOutputLimit = Mathf.Lerp(steamOutputLimit, steamOutputLimitTarget, smoothing);

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
