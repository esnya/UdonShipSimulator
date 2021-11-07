using System.Threading;
using TMPro;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(TextMeshPro))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DebugText : UdonSharpBehaviour
    {
        public int updateInterval = 10;
        public Transform target;

        private Vector3 prevPosition;
        private float prevTime;
        private TextMeshPro textMesh;
        private void Start()
        {
            if (target == null) target = transform;
            textMesh = GetComponent<TextMeshPro>();
        }

        private void Update()
        {
            if (Time.frameCount % updateInterval != 0) return;

            var time = Time.time;
            var position = target.position;
            var speed = Vector3.ProjectOnPlane(position - prevPosition, Vector3.up).magnitude / (time - prevTime) * 1.94384f;
            prevTime = time;
            prevPosition = position;

            textMesh.text = $"{speed:0.0}kt";
        }
    }
}
