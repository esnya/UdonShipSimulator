using System;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    /// <summary>
    /// Ocean water spesification.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Ocean : UdonSharpBehaviour
    {
        public const float OceanRho = 1025.0f;
        public const float OceanMu = 0.00122f;

        public const float WaterCp = 4182.0f;
        public const float AtmosphericTemperature = 20.0f;
        public const float AtmosphericPressure = 1013.25f * 100.0f;

        /// <summary>
        /// Density in kg/m^3.
        /// </summary>
        public float rho = OceanRho;

        /// <summary>
        /// Viscosity in Pa/s.
        /// </summary>
        public float mu = OceanMu;

        /// <summary>
        /// Heat capacity in J/(kg･K).
        /// </summary>
        public float cp = WaterCp;

        /// <summary>
        /// Atmospheric temperature in ℃.
        /// </summary>
        public float ta = AtmosphericTemperature;

        /// <summary>
        /// Atmospheric pressure in Pa.
        /// </summary>
        public float pa = AtmosphericPressure;

        /// <summary>
        /// Kinematic viscossity in m^2/s.
        /// </summary>
        public float Myu => rho / mu;

        /// <summary>
        /// Normalized tide strength. +1.0 is maximum tied level, -1.0 is minimum tied level.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float tide = 0.0f;

        /// <summary>
        /// Normalized tide flow strength. -1.0 to 1.0.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float tideFlow = 0.0f;

        /// <summary>
        /// Enable automatically tide simulation.
        /// </summary>
        public bool autoTide = true;

        /// <summary>
        /// Initialize tide as random.
        /// </summary>
        public bool randomTide = true;

        public float tideTime;

        private const float w = Mathf.PI / 21600.0f;

        private void Start()
        {
            enabled = false;

            if (autoTide) tideTime = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
            else if (randomTide)
            {
                tide =  UnityEngine.Random.Range(-1.0f, 1.0f);
                tideFlow = UnityEngine.Random.Range(-1.0f, 1.0f);
                tideTime = UnityEngine.Random.Range(0.0f, 12.0f);
            }

            enabled = autoTide;
        }

        private void Update()
        {
            tideTime = (tideTime + Time.deltaTime) % 43200.0f;
            tide = Mathf.Sin(tideTime * w);
            tideFlow = Mathf.Cos(tideTime * w);
        }
    }
}
