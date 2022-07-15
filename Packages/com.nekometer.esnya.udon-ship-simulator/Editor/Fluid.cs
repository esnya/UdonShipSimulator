using UnityEngine;

namespace USS2
{
    /// <summary>
    /// Fuild spesification.
    /// </summary>
    public struct Fluid
    {
        /// <summary>
        /// Density.
        /// </summary>
        public float ρ;
        /// <summary>
        /// Viscosity.
        /// </summary>
        public float μ;

        /// <summary>
        /// Kinematic Viscosity.
        /// </summary>
        public float ν => ρ / μ;

        /// <summary>
        /// Get Reynolds' number.
        /// </summary>
        /// <param name="l">Length.</param>
        /// <param name="v">Speed.</param>
        /// <returns></returns>
        public float GetRn(float l, float v)
        {
            return v * l * ρ / μ;
        }

        /// <summary>
        /// Get force from registance coefficient.
        /// </summary>
        /// <param name="cr">Resistance coefficient.</param>
        /// <param name="s">Watted surface area.</param>
        /// <param name="v">Speed.</param>
        /// <returns>Resistance force.</returns>
        public float GetRegistanceForce(float cr, float s, float v)
        {
            return 0.5f * ρ * Mathf.Pow(v, 2.0f) * s * cr;
        }

        /// <summary>
        /// Get coefficient from resistance force.
        /// /// </summary>
        /// <param name="r">Resistance force.</param>
        /// <param name="s">Watted surface area.</param>
        /// <param name="v">Speed.</param>
        /// <returns>Resistance coefficient.</returns>
        public float GetResistanceCoefficient(float r, float s, float v)
        {
            return r / (0.5f * ρ * Mathf.Pow(v, 2.0f) * s);
        }

        /// <summary>
        /// Fluid properties of ocean water.
        /// </summary>
        public static Fluid OceanWater => new Fluid() {
            ρ = 1025.0f,
            μ = 0.00122f,
        };
    }
}
