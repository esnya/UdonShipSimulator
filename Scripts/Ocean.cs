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

        /// <summary>
        /// Density.
        /// </summary>
        public float rho = OceanRho;

        /// <summary>
        /// Viscosity.
        /// </summary>
        public float mu = OceanMu;

        /// <summary>
        /// Kinematic viscossity.
        /// </summary>
        public float Myu => rho / mu;

        private void Start()
        {
            enabled = false;
        }
    }
}
