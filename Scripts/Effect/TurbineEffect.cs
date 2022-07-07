using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(100)] // After Turbine
    public class TurbineEffect : UdonSharpBehaviour
    {
        [NotNull] public SteamTurbine turbine;

        [Header("Indicators")]
        public Transform rpmIndicator;
        public Vector3 rpmIndicatorAxis = Vector3.forward;
        public float rpmIndicatorRotationScale = 0.1f;

        [Header("Particles")]
        public ParticleSystem smokeParticle;
        [NotNull] public AnimationCurve particleRateCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [Header("Sounds")]
        public AudioSource audioSource;
        [NotNull] public AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0.0f, 0.5f, 1.0f, 1.0f);
        [NotNull] public AnimationCurve pitchCurve = AnimationCurve.Linear(0.0f, 0.8f, 1.0f, 1.2f);
        public float pitchVariation = 0.02f;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(RPM))] private float _rpm;
        private float maxRPM;
        private bool reversed;
        private Shaft shaft;
        private float pitchMultiplier;
        private float audioStartPosition;
        private bool hasParticle;
        private ParticleSystem.EmissionModule smokeEmission;
        private float emissionRate;

        private float RPM {
            get => _rpm;
            set {
                _rpm = value;

                if (rpmIndicator) rpmIndicator.localRotation = Quaternion.AngleAxis(value * rpmIndicatorRotationScale, rpmIndicatorAxis);

                if (audioSource)
                {
                    var stopped = Mathf.Approximately(value, 0.0f);
                    var t = value / maxRPM;

                    if (!stopped)
                    {
                        audioSource.volume = volumeCurve.Evaluate(t);
                        audioSource.pitch = pitchCurve.Evaluate(t) * pitchMultiplier;
                    }

                    if (audioSource.isPlaying == stopped)
                    {
                        if (stopped) audioSource.Stop();
                        else {
                            audioSource.time = audioStartPosition * audioSource.clip.length;
                            audioSource.Play();
                        }
                    }


                    if (hasParticle)
                    {
                        smokeEmission.rateOverTimeMultiplier = particleRateCurve.Evaluate(t) * emissionRate;
                    }
                }
            }
        }

        private void Start()
        {
            maxRPM = turbine.rpm;
            reversed = turbine.reversed;
            shaft = turbine.shaft;
            pitchMultiplier = Random.Range(1.0f - pitchVariation, 1.0f + pitchVariation);
            audioStartPosition = Mathf.Clamp01(Random.value);

            if (hasParticle = smokeParticle != null)
            {
                smokeEmission = smokeParticle.emission;
                emissionRate = smokeEmission.rateOverTimeMultiplier;
            }

            RPM = 0.0f;
        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                RPM = Mathf.Max((reversed ? -1.0f : 1.0f) * shaft.n * 60.0f, 0.0f);
            }
        }
    }
}
