namespace USS2
{
    public struct HullCoefficient
    {
        /// <summary>
        /// (1 + k) is form factor.
        /// </summary>
        public float k;

        /// <summary>
        /// Friction coefficient.
        /// </summary>
        public float cf;

        /// <summary>
        /// Coefficient by roughness.
        /// </summary>
        public float ca;
    }
}
