using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SteamTurbine : UdonSharpBehaviour
    {
        [Header("Spec")]
        /// <summary>
        /// Maximum power in watts.
        /// </summary>
        [Min(0.0f)] public float power = 1.92e+07f;

        /// <summary>
        /// Maximum rpm.
        /// </summary>
        [Min(0.0f)] public float rpm = 380.0f;

        /// <summary>
        /// Minimum power in watts.
        /// </summary>
        [Min(0.0f)] public float minimumPower = 1.92e+07f * 0.1f;

        /// <summary>
        /// Is reversed.
        /// </summary>
        public bool reversed;

        [Header("Output")]
        [NotNull] public Shaft shaft;

        [Header("Input")]
        /// <summary>
        /// Normalized steam input in -1.0 to 1.0.
        /// </summary>
        [Range(0, 1.0f)] public float input;

        private float powerToTorque;

        private void Start()
        {
            powerToTorque = 60.0f / (2.0f * Mathf.PI * rpm);
        }

        public void Update()
        {
            shaft.inputTorque += GetAvailableTorque(input);
        }

        [PublicAPI]
        public float GetAvailableTorque(float i)
        {
            var p = power * i;
            if (p < minimumPower) return 0.0f;
            return p * powerToTorque * (reversed ? -1.0f : 1.0f);
        }
    }
}
