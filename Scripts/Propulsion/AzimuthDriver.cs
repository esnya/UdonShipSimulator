
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class AzimuthDriver : UdonSharpBehaviour
    {
        /// <summary>
        /// Rotation axis.
        /// </summary>
        public Vector3 axis = Vector3.up;

        /// <summary>
        /// Response of input.
        /// </summary>
        public float response = 0.1f;

        /// <summary>
        /// Max speed of rotation in degrees per second.
        /// </summary>
        public float maxRotationSpeed = 180.0f / 10.0f;

        /// <summary>
        /// Target azimuth input.
        /// </summary>
        [NonSerialized] public float targetAzimuth;

        private float azimuthVelocity;

        /// <summary>
        /// Current azimuth.
        /// </summary>
        [NonSerialized][UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Azimuth))] public float _azimuth;

        /// <summary>
        /// Current azimuth.
        /// </summary>
        public float Azimuth {
            get => _azimuth;
            private set {
                _azimuth = value;
                transform.localRotation = Quaternion.AngleAxis(_azimuth, axis);
            }
        }

        private void Start()
        {
            _USS_Respawned();
        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject)) Owner_Update();
        }

        private void Owner_Update()
        {
            Azimuth = Mathf.SmoothDampAngle(Azimuth, targetAzimuth, ref azimuthVelocity, response, maxRotationSpeed);
        }

        public void _USS_Respawned()
        {
            targetAzimuth = 0.0f;
            Azimuth = 0.0f;
        }
    }
}
