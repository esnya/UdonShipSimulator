using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Engine : UdonSharpBehaviour
    {
        [Header("Specs")]
        /// <summary>
        /// Maximum Power in kW.
        /// </summary>
        public float power = 1700.0f * 1000.0f;

        /// <summary>
        /// Maximum RPM of crank shaft.
        /// </summary>
        public float rpm = 720.0f;

        /// <summary>
        /// Gear ratio of reducer.
        /// </summary>
        public float gearRatio = 3.22f;

        /// <summary>
        /// Normalized torque curve vs RPM. t is normalized RPM 0-1, value is normalize torque 0-1.
        /// </summary>
        public AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [Header("Effects")]
        /// <summary>
        /// Sound of engine.
        /// </summary>
        public AudioSource audioSource;

        /// <summary>
        /// Curve of sound pitch by normalized RPM.
        /// </summary>
        public AnimationCurve pitchCurve = AnimationCurve.Linear(0.0f, 0.8f, 1.0f, 1.2f);

        /// <summary>
        /// Curve of sound volume by normalized RPM.
        /// </summary>
        public AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0.0f, 0.8f, 1.0f, 1.0f);

        /// <summary>
        /// Random variation range of pitch for each engine.
        /// </summary>
        public float pitchVariation = 0.1f;

        /// <summary>
        /// Smoke effect from funnel.
        /// </summary>
        public ParticleSystem smokeParticle;

        /// <summary>
        /// Curve of smoke effect emission rate over time.
        /// </summary>
        public float smokeParticleEmissionCurve = 0.2f;


        [Header("Output")]
        /// <summary>
        /// Output shaft. Get from parents if null.
        /// </summary>
        public Shaft shaft;

        [Header("Runtime Status")]
        /// <summary>
        /// Throttle input. 0-1.
        /// </summary>
        [NonSerialized] public float throttle;

        /// <summary>
        /// Revolution per second.
        /// </summary>
        [NonSerialized] public float n;

        private float maxTorque;
        private float volume;
        private float pitch;
        private ParticleSystem.EmissionModule smokeParticleEmission;
        private float smokeParticleEmissionOverTimeRate;

        private void Start()
        {
            if (!shaft) shaft = GetComponentInParent<Shaft>();

            maxTorque = power / rpm * gearRatio;

            if (!audioSource) audioSource = GetComponentInChildren<AudioSource>();
            if (audioSource)
            {
                volume = audioSource.volume;
                pitch = audioSource.pitch * (1.0f + UnityEngine.Random.Range(-pitchVariation, pitchVariation));
            }

            if (!smokeParticle) smokeParticle = GetComponentInChildren<ParticleSystem>();
            if (smokeParticle)
            {
                smokeParticleEmission = smokeParticle.emission;
                smokeParticleEmissionOverTimeRate = smokeParticleEmission.rateOverTimeMultiplier;
            }
        }

        private void Update()
        {
            n = shaft.n * gearRatio;

            if (Networking.IsOwner(gameObject)) Owner_Update();

            var normalizedRPM = Mathf.Abs(n / (rpm / 60.0f));
            var stopped = Mathf.Approximately(normalizedRPM, 0.0f);
            if (audioSource)
            {
                if (stopped)
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                }
                else
                {
                    audioSource.pitch = pitchCurve.Evaluate(normalizedRPM) * pitch;
                    audioSource.volume = volumeCurve.Evaluate(normalizedRPM) * volume;

                    if (!audioSource.isPlaying)
                    {
                        if (audioSource.clip) audioSource.time = UnityEngine.Random.Range(0.0f, audioSource.clip.length);
                        audioSource.Play();
                    }
                }
            }

            if (smokeParticle)
            {
                smokeParticleEmission.rateOverTimeMultiplier = smokeParticleEmissionOverTimeRate * Mathf.Pow(normalizedRPM, smokeParticleEmissionCurve);
            }
        }

        private void Owner_Update()
        {
            var normalizedRPM = n / (rpm / 60.0f);
            shaft.inputTorque += torqueCurve.Evaluate(normalizedRPM) * maxTorque * throttle;
        }
    }
}
