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
        /// Maximum power for reverse in watts.
        /// </summary>
        [Min(0.0f)] public float reversePower = 1.92e+07f * 0.5f;

        /// <summary>
        /// Resistance of rotation such as flywheel.
        /// </summary>
        [Min(1.0f)] public float shaftMomentOfInertia = 1000000.0f;

        [Header("Output")]
        [NotNull] public UdonSharpBehaviour shaft;
        [NotNull] public string shaftRevolutionVariable = "n";
        [NotNull] public string shaftLoadVariable = "propellerLoad";
        [NotNull] public string efficiencyVariable = "efficiency";

        [Header("Input")]
        /// <summary>
        /// Normalized steam input in -1.0 to 1.0.
        /// </summary>
        [Range(-1.0f, 1.0f)] public float input;

        [Header("State")]
        /// <summary>
        /// Revolution of shaft in 1/s.
        /// </summary>
        public float n;

        private float powerToTorque;

        private void Start()
        {
            powerToTorque = 60.0f / (2.0f * Mathf.PI * rpm);
        }

        public void Update()
        {
            var qr = (float)shaft.GetProgramVariable(shaftLoadVariable);
            var qa = GetAvailableTorque(input);
            var eta = (float)shaft.GetProgramVariable(efficiencyVariable);
            n += (qa * eta - qr) * Time.deltaTime / shaftMomentOfInertia;

            if (float.IsInfinity(n) || float.IsNaN(n)) n = 0.0f;
            shaft.SetProgramVariable(shaftRevolutionVariable, n);
        }

        public void _USS_Respawned()
        {
            n = 0.0f;
        }

        [PublicAPI]
        public float GetAvailableTorque(float i)
        {
            var reversed = i < 0.0f;
            var p = (reversed ? reversePower : power) * i;
            if (Mathf.Abs(p) < minimumPower) return 0.0f;
            return p * powerToTorque;
        }
    }
}