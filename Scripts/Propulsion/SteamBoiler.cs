using System;
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SteamBoiler : UdonSharpBehaviour
    {
        public const float KGF_CM2 = 98066.5f; // Pa/(kgf/cm^2)
        public const float SHP = 745.7000f; // W/SHP
        public const float Hour = 60.0f; // s/h

        [Header("Burner")]

        /// <summary>
        /// Fuel consumption in kg/s.
        /// </summary>
        [Min(0.0f)] public float fuelConsumpsion = 5.0f * 1000.0f / 60.0f; // 8.0f * Mathf.Pow(40.0f, 3.0f / 2.0f) * / 60.0f;

        /// <summary>
        /// Heat of combustion in J/kg.
        /// </summary>
        [Min(0.0f)] public float fuelHeatingValue = 45.0f * 1000000.0f;

        /// <summary>
        /// Efficiency how fuel heats water.
        /// </summary>
        [Range(0.0f, 1.0f)] public float thermalEfficiency = 0.95f;

        [Header("Water & Steam")]
        /// <summary>
        /// Max output steam flow in kg/s.
        /// </summary>
        [Min(0.0f)] public float maxSteamFlow = 87.86f * 1000.0f / 60.0f;

        /// <summary>
        /// Maximum pressuer of steam in Pa.
        /// </summary>
        [Min(0.0f)] public float maxPressure = 30.0f * 98066.5f;

        /// <summary>
        /// Maximum temperature of steam in ℃.
        /// </summary>
        [Min(0.0f)] public float maxTemperature = 350.0f;

        /// <summary>
        /// Water tank capacity in kg.
        /// </summary>
        [Min(0.0f)] public float capacity = 11.0f * 1000.0f;

        /// <summary>
        /// Head discharge in W/K
        /// </summary>
        [Min(0.0f)] public float heatDischarge = 10.0f;

        /// <summary>
        /// Heat capacity ratio of tank stracture vs water in tank.
        /// </summary>
        public float tankHeatCapacityRatio = 10.0f;

        /// <summary>
        /// Steam flow in rerief valve in kg/s
        /// </summary>
        public float steamReriefFlow = 87.86f * 1000.0f / 60.0f;

        public float steamReriefResponse = 0.1f;
        public float pressureCurve = 2.0f;

        [Header("Output")]
        /// <summary>
        /// Output pipe. Get from parents if null.
        /// </summary>
        public SteamPipe steamPipe;

        [Header("Effects")]
        public AudioSource sound;
        public float soundPitchVariation = 0.1f;

        [ListView("Particles")] public ParticleSystem[] particles = { };
        [ListView("Particles")][Popup("GetParticleTypes")] public int[] particleTypes = { };


        [Header("Runtime Status")]
        [Range(0.0f, 1.0f)] public float fuelValveValue = 0.0f;
        [Range(0.0f, 1.0f)] public float steamValveValue = 0.0f;

        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float pressure = 0.0f;
        private float steamReriefValveValue;
        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float temperature = 0.0f;
        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float steamRefiefedFlow;
        [UdonSynced(UdonSyncMode.Smooth)][NonSerialized] public float steamFlow;
        [NonSerialized] public float fuelFlow;

        private float[] particleEmisionRates;
        private float cp = Ocean.WaterCp;
        private float ta = Ocean.AtmosphericTemperature;
        private float pa = Ocean.AtmosphericPressure;

        private void Start()
        {
            if (!steamPipe) steamPipe = GetComponentInParent<SteamPipe>();

            particleEmisionRates = new float[particles.Length];
            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle) continue;
                particleEmisionRates[i] = particle.emission.rateOverTimeMultiplier;
            }

            var ocean = GetComponentInParent<Ocean>();
            if (ocean)
            {
                cp = ocean.cp;
                ta = ocean.ta;
                pa = ocean.pa;
            }

            _USS_Respawned();
        }

        private void Update()
        {
            fuelFlow = fuelValveValue * fuelConsumpsion;
            if (Networking.IsOwner(gameObject)) Owner_Update();

            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle) continue;

                var emission = particle.emission;
                emission.rateOverTimeMultiplier = particleEmisionRates[i] * GetParticleValue(particleTypes[i]);
            }

            if (sound)
            {
                var play = !Mathf.Approximately(fuelValveValue, 0.0f);
                if (play)
                {
                    sound.volume = Mathf.Pow(Mathf.Clamp01(fuelValveValue), 0.5f);
                    if (!sound.isPlaying) {
                        sound.pitch = UnityEngine.Random.Range(1.0f - soundPitchVariation, 1.0f + soundPitchVariation);
                        sound.time = UnityEngine.Random.Range(0.0f, sound.clip.length);
                        sound.Play();
                    }
                }
                else
                {
                    if (sound.isPlaying) sound.Stop();
                }
            }
        }

        private void Owner_Update()
        {
            var deltaTime = Time.deltaTime;

            var normalizedFlow = GetNormalizedFlow();
            pressure = Mathf.LerpUnclamped(pa, maxPressure, normalizedFlow);
            steamReriefValveValue = Mathf.Lerp(steamReriefValveValue, pressure >= maxPressure * 1.01f ? 1.0f : 0.0f, deltaTime * steamReriefResponse);
            steamRefiefedFlow = steamReriefFlow * steamReriefValveValue; // kg/s
            steamFlow = maxSteamFlow * steamValveValue * Mathf.Clamp01(normalizedFlow) * steamPipe.steamInputLimit; // kg/s

            var deltaEnthalpy = (GetHeatingEnthalpyPerSeconds() - GetDischargedEnthalpyPerSeconds() - GetOutputEnthalpyPerSeconds()) * deltaTime; // J
            temperature += deltaEnthalpy / (capacity * cp * tankHeatCapacityRatio); // K

            steamPipe.steamInput += steamFlow;
        }

        private float AugustSWVP(float t)
        {
            return 6.1078f * Mathf.Pow(10.0f, 7.5f * t / (t + 237.15f)); // Pa
        }

        private float GetNormalizedFlow()
        {
            return Mathf.Pow(Mathf.Max((temperature - 100) / (maxTemperature - 100), 0.0f), pressureCurve);
        }

        private float GetHeatingEnthalpyPerSeconds()
        {
            return fuelFlow * fuelHeatingValue;
        }

        private float GetDischargedEnthalpyPerSeconds()
        {
            return heatDischarge * (temperature - ta);
        }

        private float GetOutputEnthalpyPerSeconds()
        {
            return (steamFlow + steamRefiefedFlow) * (temperature - ta) * cp;
        }

        public void _USS_Respawned()
        {
            fuelValveValue = 0.0f;
            temperature = ta;
            pressure = pa;
        }

        private float GetIndicatorValue(int type)
        {
            switch (type)
            {
                case 0:
                    return temperature;
                case 1:
                    return Mathf.Min(pressure, maxPressure);
                case 2:
                    return fuelValveValue * fuelConsumpsion;
                default:
                    return 0.0f;
            }
        }

        private float GetParticleValue(int type)
        {
            switch (type)
            {
                case 0:
                    return fuelValveValue;
                case 1:
                    return Mathf.Max(pressure - maxPressure, 0.0f);
                default:
                    return 0.0f;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public string[] GetParticleTypes() => new [] {
            "Smoke",
            "ReriefedSteam",
        };
#endif
    }
}
