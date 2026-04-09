using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HullSpeedEffect : UdonSharpBehaviour
    {
        /// <summary>
        /// Smoothing speed to reduce jitter by sync.
        /// </summary>
        public float smoothing = 1.0f;

        [Header("Particles")]
        /// <summary>
        /// Particle effects
        /// </summary>
        [NotNull] public ParticleSystem[] particles = { };

        /// <summary>
        /// Max speed in m/s.
        /// </summary>
        [NotNull] public float[] maxEmissionSpeeds = { };

        /// <summary>
        /// Curve of emission rate.
        /// </summary>
        [NotNull] public float[] emissionRateCurves = { };

        /// <summary>
        /// Keep sealevel.
        /// </summary>
        public bool[] keepSeaLevels = { };

        /// <summary>
        /// Hull speed in m/s.
        /// </summary>
        [NonSerialized] public float hullSpeed = 0.0f;

        private Rigidbody vesselRigidbody;
        private Vector3 prevPosition;
        private Transform[] particleTransforms;
        private ParticleSystem.EmissionModule[] particleEmissions;
        private float[] particleEmissionRateOverTimeMultipliers;
        private float seaLevel;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            _USS_Respawned();

            var ocean = vesselRigidbody.GetComponentInParent<Ocean>();
            if (ocean)
            {
                seaLevel = ocean.transform.position.y;
            }

            particleTransforms = new Transform[particles.Length];
            particleEmissions = new ParticleSystem.EmissionModule[particles.Length];
            particleEmissionRateOverTimeMultipliers = new float[particles.Length];
            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle) continue;
                particleTransforms[i] = particle.transform;
                var emission = particleEmissions[i] = particles[i].emission;
                particleEmissionRateOverTimeMultipliers[i] = emission.rateOverTimeMultiplier;
            }
        }

        private void Update()
        {
            if (!vesselRigidbody) return;

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0.0f) return;
            var position = vesselRigidbody.position;

            hullSpeed = Mathf.Lerp(hullSpeed, Vector3.Distance(position, prevPosition) / deltaTime, deltaTime / smoothing);

            prevPosition = position;

            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle) continue;

                var t = particleTransforms[i];
                if (i < keepSeaLevels.Length && keepSeaLevels[i])
                {
                    var particlePosition = t.position;
                    particlePosition.y = seaLevel;
                    t.position = particlePosition;
                }
                t.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.ProjectOnPlane(t.forward, Vector3.up));

                var emission = particleEmissions[i];
                var maxEmissionSpeed = i < maxEmissionSpeeds.Length ? maxEmissionSpeeds[i] : 0.0f;
                var emissionRateCurve = i < emissionRateCurves.Length ? emissionRateCurves[i] : 1.0f;
                emission.rateOverTime = maxEmissionSpeed > 0.0f
                    ? particleEmissionRateOverTimeMultipliers[i] * Mathf.Pow(Mathf.Clamp01(hullSpeed / maxEmissionSpeed), emissionRateCurve)
                    : 0.0f;
            }
        }

        public void _USS_Respawned()
        {
            hullSpeed = 0.0f;
            prevPosition = vesselRigidbody.position;
        }
    }
}
