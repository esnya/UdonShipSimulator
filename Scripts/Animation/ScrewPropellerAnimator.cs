using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class ScrewPropellerAnimator : UdonSharpBehaviour
    {
        public ScrewPropeller screwPropeller;
        public Vector3 axis = Vector3.forward;

        private ParticleSystem.MainModule particleMain;
        private ParticleSystem.EmissionModule particleEmission;
        private bool hasParticle;
        private float rateOverTimeMultiplier;
        public float particleRateCurve = 100.0f;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(N))] private float _n;
        private float N
        {
            get => _n;
            set
            {
                _n = value;

                if (hasParticle)
                {
                    var nn = Mathf.Clamp(_n / screwPropeller.maxRPM, -1.0f, 1.0f);
                    particleEmission.rateOverTimeMultiplier = rateOverTimeMultiplier * ParticleRateCurve(Mathf.Abs(nn), particleRateCurve);
                }
            }
        }

        private float _angle;
        private float propellerPitch;

        private float Angle
        {
            get => _angle;
            set {
                _angle = value % 360.0f;
                transform.localRotation = Quaternion.AngleAxis(_angle, axis);
            }
        }

        private void Start()
        {
            var particleSystem = GetComponent<ParticleSystem>();
            if (hasParticle = particleSystem)
            {
                particleMain = particleSystem.main;

                particleEmission = particleSystem.emission;
                rateOverTimeMultiplier = particleEmission.rateOverTimeMultiplier;
            }

            propellerPitch = screwPropeller.pitch;
        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                N = screwPropeller.nr;
            }

            Angle += N * 360.0f * Time.deltaTime;

            if (hasParticle)
            {
                particleMain.startSpeedMultiplier = N * propellerPitch;
            }
        }

        public void _USS_Respawned()
        {
            N = 0.0f;
            Angle = 0.0f;
        }

        private float ParticleRateCurve(float x, float a)
        {
            return (1 + a) / (1 + a * x) * x;
        }
    }
}
