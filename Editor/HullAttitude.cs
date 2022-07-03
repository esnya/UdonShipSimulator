namespace USS2
{
    public struct HullAttitude
    {
        public float lcb;

        public float tf;
        public float ta;

        public float T => (ta + tf) * 0.5f;
    }
}
