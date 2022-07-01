using UnityEngine;

namespace USS2
{
    /// <summary>
    /// Estimate resistance coefficient of vessel hull by ITTC 1957 formula.
    /// </summary>
    public class ITTC1957
    {
        /// <summary>
        /// Get frictional resistance coefficient.
        /// </summary>
        /// <param name="rn">Raynolds' number.</param>
        /// <returns>Ffrictional resistance coefficient</returns>
        public static float GetCF(float rn)
        {
            return 0.075f / Mathf.Pow((Mathf.Log(rn) / Mathf.Log(10)) - 2.0f, 2.0f);
        }
    }
}
