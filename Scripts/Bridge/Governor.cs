
using UdonSharp;
using UdonToolkit;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Governor : UdonSharpBehaviour
    {
        public SteamTurbine turbine;
        [SerializeField][UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Value))] private float _value;
        public float increaseStep = 0.05f;

        [ListView("Visual Transforms")] public Transform[] visualTransforms = { };
        [ListView("Visual Transforms")] public float[] rotationScales = { };
        [ListView("Visual Transforms")] public Vector3[] rotationAxises = { };

        private float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp(value, -1.0f, 1.0f);
                if (turbine) turbine.input = _value;
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
            Value = turbine ? turbine.input : Value;
        }

        public void _USS_Respawned()
        {
            Value = 0.0f;
        }

        public void Increase()
        {
            Value += increaseStep;
        }

        public void Decrease()
        {
            Value -= increaseStep;
        }
    }
}
