
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Valve : UdonSharpBehaviour
    {
        /// <summary>
        /// Target udon behaviour.
        /// </summary>
        [NotNull] public UdonSharpBehaviour target;

        /// <summary>
        /// Target variable name of target.
        /// </summary>
        [Popup("programVariable", "@target")] public string variableName = "valveValue";

        /// <summary>
        /// Initial value. 0 to 1. Target varible will be set as (value + valueBias) * valueMultiplier.
        /// </summary>
        [SerializeField][UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))][Range(0.0f, 1.0f)] private float _value;

        /// <summary>
        /// Bias of value.
        /// </summary>
        [Range(0.0f, 1.0f)] public float valueBias = 0.0f;

        /// <summary>
        /// Multiplier of value.
        /// </summary>
        public float valueMultiplier = 1.0f;

        /// <summary>
        /// Step for Increase/Decrease.
        /// </summary>
        public float increaseStep = 1.0f / 720.0f;

        /// <summary>
        /// Step for FastIncrease/FastDecrease.
        /// </summary>
        public float fastIncreaseStep = 0.05f;

        /// <summary>
        /// Viaual transform to animate such as handle.
        /// </summary>
        [ListView("Visual Transforms")] public Transform[] visualTransforms = { };

        /// <summary>
        /// Remapping scale for visual transforms.
        /// </summary>
        [ListView("Visual Transforms")] public float[] rotationScales = { };

        /// <summary>
        /// Rotation axis to animate visual transform.
        /// </summary>
        [ListView("Visual Transforms")] public Vector3[] rotationAxises = { };
        private float initialValue;

        private float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                if (target) target.SetProgramVariable(variableName, (_value + valueBias) * valueMultiplier);
                for (var i = 0; i < visualTransforms.Length; i++)
                {
                    var t = visualTransforms[i];
                    if (!t) continue;

                    t.localRotation = Quaternion.AngleAxis(_value * rotationScales[i], rotationAxises[i]);
                }
            }
        }

        private void Start()
        {
            initialValue = Value;
            _USS_Respawned();
        }

        public void _USS_Respawned()
        {
            Value = initialValue;
        }

        [PublicAPI] public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        [PublicAPI] public void Increase()
        {
            _TakeOwnership();
            Value += increaseStep;
        }

        [PublicAPI] public void Decrease()
        {
            _TakeOwnership();
            Value -= increaseStep;
        }

        [PublicAPI] public void IncreaseFast()
        {
            _TakeOwnership();
            Value += fastIncreaseStep;
        }

        [PublicAPI] public void DecreaseFast()
        {
            _TakeOwnership();
            Value -= fastIncreaseStep;
        }

        [PublicAPI] public void Set0()
        {
            _TakeOwnership();
            Value = 0;
        }
    }
}
