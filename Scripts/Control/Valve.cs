﻿
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
        [NotNull] public UdonSharpBehaviour target;
        [Popup("programVariable", "@target")] public string variableName = "valveValue";
        [SerializeField][UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] private float _value;
        public float increaseStep = 1.0f / 720.0f;
        public float fastIncreaseStep = 0.05f;

        [ListView("Visual Transforms")] public Transform[] visualTransforms = { };
        [ListView("Visual Transforms")] public float[] rotationScales = { };
        [ListView("Visual Transforms")] public Vector3[] rotationAxises = { };

        private float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                if (target) target.SetProgramVariable(variableName, _value);
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
            Value = target ? (float)target.GetProgramVariable(variableName) : Value;
        }

        public void _USS_Respawned()
        {
            Value = 0.0f;
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
