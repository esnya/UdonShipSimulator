namespace USS2
{
    public struct PropellerDimension
    {
        /// <summary>
        /// Propeller diameter in meters.
        /// </summary>
        public float d;

        /// <summary>
        /// Propeller pitch in meters.
        /// </summary>
        public float p;

        /// <summary>
        /// Number of blades.
        /// </summary>
        public int z;

        /// <summary>
        /// Constant factor by number of screw propellers.
        ///
        /// 0 to 0.1 to twin-screw.
        /// 0.2 for single screw.
        /// </summary>
        public float k;

        /// <summary>
        /// Propeller blade surface roughness. 0.0003 m for new propellers.
        /// </summary>
        public float kp;

        /// <summary>
        /// Diameter of boss in meters.
        /// </summary>
        public float db;

        /// <summary>
        /// Blade area ratio.
        /// </summary>
        public float aeao;
    }
}
