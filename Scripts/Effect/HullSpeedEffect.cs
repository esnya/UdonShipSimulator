using System;
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
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
        [NotNull][ListView("Particles")] public ParticleSystem[] particles = { };

        /// <summary>
        /// Max speed in m/s.
        /// </summary>
        [NotNull][ListView("Particles")] public float[] maxEmissionSpeeds = { };

        /// <summary>
        /// Curve of emission rate.
        /// </summary>
        [NotNull][ListView("Particles")] public float[] emissionRateCurves = { };

        /// <summary>
        /// Keep sealevel.
        /// </summary>
        [ListView("Particle")] public bool[] keepSeaLevels = { };

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
            var deltaTime = Time.deltaTime;
            var position = vesselRigidbody.position;

            hullSpeed = Mathf.Lerp(hullSpeed, Vector3.Distance(position, prevPosition) / deltaTime, deltaTime / smoothing);

            prevPosition = position;

            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (!particle) continue;

                var t = particleTransforms[i];
                t.position = Vector3.Scale(t.position, Vector3.one + Vector3.up * (seaLevel  - 1.0f));
                t.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.ProjectOnPlane(t.forward, Vector3.up));

                var emission = particleEmissions[i];
                emission.rateOverTime = particleEmissionRateOverTimeMultipliers[i] * Mathf.Pow(Mathf.Clamp01(hullSpeed / maxEmissionSpeeds[i]), emissionRateCurves[i]);
            }
        }

        public void _USS_Respawned()
        {
            hullSpeed = 0.0f;
            prevPosition = vesselRigidbody.position;
        }
    }
}