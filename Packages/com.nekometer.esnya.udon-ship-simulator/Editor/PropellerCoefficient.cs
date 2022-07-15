namespace USS2
{
    public struct PropellerCoefficient
    {
        public float kT;
        public float kQ;

        public float w;
        public float t;

        /// <summary>
        /// Open water efficiency.
        /// </summary>
        public float eta0;

        /// <summary>
        /// Relative rotative efficiency.
        /// </summary>
        public float etaR;

        /// <summary>
        /// Shafting efficiency.
        /// </summary>
        public float etaS;
    }
}
