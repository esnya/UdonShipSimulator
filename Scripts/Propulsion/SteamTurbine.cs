using System;
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SteamTurbine : UdonSharpBehaviour
    {
        [Header("Spec")]
        /// <summary>
        /// Maximum power in watts.
        /// </summary>
        [Min(0.0f)] public float power = 1.92e+07f;

        /// <summary>
        /// Maximum rpm.
        /// </summary>
        [Min(0.0f)] public float rpm = 380.0f * 8.0f;

        /// <summary>
        /// Gear ratio of reduction drive.
        /// </summary>
        public int gearRatio = 8;

        /// <summary>
        /// Minimum power vs maximum power.
        /// </summary>
        [Range(0.0f, 1.0f)] public float minimumPowerRatio = 0.05f;

        /// <summary>
        /// Steam consumption on max power in kg/s.
        /// </summary>
        [Min(0.0f)] public float steamConsumption = 87.86f * 1000.0f / 60.0f * 3.0f / 2.0f;

        /// <summary>
        /// Is reversed.
        /// </summary>
        public bool reversed;

        [Header("Input")]
        /// <summary>
        /// Input steam pipe. Get from parents if null.
        /// </summary>
        public SteamPipe steamPipe;

        [Header("Output")]
        [NotNull] public Shaft shaft;

        [Header("Effects")]
        public AudioSource audioSource;
        public float volumeCurve = 0.5f;
        public float pitchCurve = 1.0f;
        public float pitchRange = 0.2f;
        public float pitchVariation = 0.02f;

        [Header("Runtime Status")]
        /// <summary>
        /// Steam input valve.
        /// </summary>
        [Range(0.0f, 1.0f)] public float steamValveValue = 0.0f;
        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float n;
        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float steamFlow;

        private GameObject vesselGameObject;
        private float powerToTorque;
        private float pitchMultiplier;

        private void Start()
        {
            vesselGameObject = GetComponentInParent<Rigidbody>().gameObject;
            if (!steamPipe) steamPipe = GetComponentInParent<SteamPipe>();


            powerToTorque = 60.0f * gearRatio / (2.0f * Mathf.PI * rpm);
        }

        private void Update()
        {
            if (Networking.IsOwner(vesselGameObject)) Owner_Update();

            n = shaft.n * gearRatio;

            if (audioSource)
            {
                var t = Mathf.Clamp01(Mathf.Abs(n * 60.0f / rpm));
                var stopped = Mathf.Approximately(t, 0.0f);

                if (!stopped)
                {
                    audioSource.volume = Mathf.Pow(t, volumeCurve);
                    audioSource.pitch = (1.0f + (Mathf.Pow(t, pitchCurve) * 2.0f - 1.0f) * pitchRange) * pitchMultiplier;
                }

                if (audioSource.isPlaying == stopped)
                {
                    if (stopped) audioSource.Stop();
                    else {
                        pitchMultiplier = 1.0f + (UnityEngine.Random.value * 2.0f - 1.0f) * pitchVariation;
                        audioSource.time = UnityEngine.Random.Range(0, audioSource.clip.length);
                        audioSource.Play();
                    }
                }
            }
        }

        private void Owner_Update()
        {
            steamPipe.steamOutput += steamConsumption * steamValveValue;
            steamFlow = Mathf.Clamp(steamPipe.steamFlow * steamValveValue, 0.0f, steamConsumption) * steamPipe.steamInputLimit; // kg/s
            shaft.inputTorque += GetAvailableTorque(steamFlow / steamConsumption);
        }

        private float GetIndicatorValue(int type)
        {
            switch (type)
            {
                case 0:
                    return shaft.n * gearRatio;
                case 1:
                    return steamPipe.steamFlow * steamValveValue;
                default:
                    return 0.0f;
            }
        }

        [PublicAPI]
        public float GetAvailableTorque(float i)
        {
            if (i < minimumPowerRatio) return 0.0f;
            var p = power * i;
            return p * powerToTorque * (reversed ? -1.0f : 1.0f);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public string[] GetIndicatorTypes() {
            return new[] { "Revolution", "SteamFlow" };
        }
#endif
    }
}
