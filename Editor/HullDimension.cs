using UnityEngine;

namespace USS2
{
    public struct HullDimension
    {
        /// <summary>
        /// Length of waterline.
        /// </summary>
        public float l;

        /// <summary>
        /// Breadth of waterline.
        /// </summary>
        public float b;

        /// <summary>
        /// Draught.
        /// </summary>
        public float t;

        /// <summary>
        /// Wetted surface.
        /// </summary>
        public float s;

        /// <summary>
        /// Volume under water.
        /// </summary>
        public float v;

        /// <summary>
        /// Midship section area.
        /// </summary>
        public float am;

        /// <summary>
        /// Waterplane area.
        /// </summary>
        public float aw;

        /// <summary>
        /// Afterbody form.
        /// </summary>
        public AfterbodyForm afterbodyForm;

        /// <summary>
        /// Bulbous bow.
        /// </summary>
        public bool hasBulbousBow;

        /// <summary>
        /// Transom.
        /// </summary>
        public bool hasTransom;

        /// <summary>
        /// Midship section coefficient.
        /// </summary>
        public float CM => am / (b * t);

        /// <summary>
        /// Warterplane area coefficient.
        /// </summary>
        public float CW => aw / (b * l);

        /// <summary>
        /// Block coefficient.
        /// </summary>
        public float CB => v / (l * b * t);

        /// <summary>
        /// Prismatic coefficient.
        /// </summary>
        public float CP => v / (l * am);

        /// <summary>
        /// Vertical prismatic coefficient.
        /// </summary>
        public float CVP => v / (aw * t);

        /// <summary>
        /// Get Froude number.
        /// </summary>
        /// <param name="v">Hull speed.</param>
        /// <param name="g">Gravity force.</param>
        /// <returns>Froude number.</returns>
        public float GetFn(float v, float g)
        {
            return v / Mathf.Sqrt(l * g);
        }

        /// <summary>
        /// Get speed in m/s from Froude number.
        /// </summary>
        /// <param name="fn">Froude number.</param>
        /// <param name="g">Gravity force.</param>
        /// <returns>Speed in m/s.</returns>
        public float GetSpeedFromFn(float fn, float g)
        {
            return fn * Mathf.Sqrt(l * g);
        }
    }
}
