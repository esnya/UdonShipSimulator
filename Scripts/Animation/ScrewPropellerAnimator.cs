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
        public float minRPM = 60.0f;

        private ParticleSystem.MainModule particleMain;
        private ParticleSystem.EmissionModule particleEmission;
        private bool hasParticle;
        private float rateOverTimeMultiplier;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(N))] private float _n;
        private float N
        {
            get => _n;
            set
            {
                _n = value;

                if (hasParticle)
                {
                    particleEmission.rateOverTimeMultiplier = rateOverTimeMultiplier * Mathf.SmoothStep(0.0f, minRPM / 60.0f, Mathf.Abs(_n));
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
                N = screwPropeller.n;
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
    }
}
