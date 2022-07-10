
using UdonSharp;
using UnityEngine;
using UdonToolkit;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SpeedMeter : UdonSharpBehaviour
    {
        [ListView("Indicators")] public Transform[] indicators = { };
        [ListView("Indicators")] public float[] angleScales = { };
        [ListView("Indicators")] public Vector3[] axises = { };
        [ListView("Indicators")] public float[] clampedSpeeds = { };

        public float smoothing = 1.0f;

        private Rigidbody vesselRigidbody;
        private Vector3 prevPosition;
        private float speed = 0.0f;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var position = vesselRigidbody.position;

            speed = Mathf.Lerp(speed, Vector3.Distance(position, prevPosition) / deltaTime, deltaTime / smoothing);
            for (var i = 0; i < indicators.Length; i++)
            {
                var indicator = indicators[i];
                if (!indicator) continue;
                indicators[i].localRotation = Quaternion.AngleAxis(Mathf.Clamp(speed, 0.0f, clampedSpeeds[i]) * angleScales[i], axises[i]);
            }

            prevPosition = position;
        }

        public void _USS_Respawned()
        {
            speed = 0.0f;
        }
    }
}
