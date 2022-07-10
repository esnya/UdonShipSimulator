using UdonSharp;

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


        private void Start()
        {
            enabled = false;
        }
    }
}
