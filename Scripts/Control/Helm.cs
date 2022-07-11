
using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Helm : UdonSharpBehaviour
    {
        [Range(0.0f, 90.0f)] public float maxAngle = 30.0f;
        public Transform rudderTransform;
        public float increaseStep = 0.033333333f;
        public float fastIncreaseStep = 2.0f;

        [ListView("Visual Transforms")] public Transform[] visualTransforms = { };
        [ListView("Visual Transforms")] public float[] rotationScales = { };
        [ListView("Visual Transforms")] public Vector3[] rotationAxises = { };

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Angle))] private float _angle;

        private float Angle
        {
            get => _angle;
            set
            {
                _angle = Mathf.Clamp(value, -maxAngle, maxAngle);
                if (rudderTransform) rudderTransform.localEulerAngles = Vector3.up * _angle;
                for (var i = 0; i < visualTransforms.Length; i++)
                {
                    var t = visualTransforms[i];
                    if (!t) continue;

                    t.localRotation = Quaternion.AngleAxis(_angle * rotationScales[i], rotationAxises[i]);
                }
            }
        }

        private void Start()
        {
            Angle = rudderTransform.localEulerAngles.y;
        }

        public void _USS_Respawned()
        {
            Angle = 0.0f;
        }

        [PublicAPI] public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        [PublicAPI] public void IncreaseAngle()
        {
            _TakeOwnership();
            Angle += increaseStep;
        }

        [PublicAPI] public void DecreaseAngle()
        {
            _TakeOwnership();
            Angle -= increaseStep;
        }


        [PublicAPI] public void IncreaseFast()
        {
            _TakeOwnership();
            Angle += fastIncreaseStep;
        }

        [PublicAPI] public void DecreaseFast()
        {
            _TakeOwnership();
            Angle -= fastIncreaseStep;
        }

        [PublicAPI] public void Set0()
        {
            _TakeOwnership();
            Angle = 0;
        }
    }
}