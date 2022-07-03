using TMPro;
using UdonSharp;
using UnityEngine;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(TextMeshPro))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DebugText : UdonSharpBehaviour
    {
        public int updateInterval = 10;
        public Transform target;

        private Vector3 prevPosition;
        private float prevTime;
        private TextMeshPro textMesh;
        private int updateOffset;

        private void Start()
        {
            if (target == null) target = transform;
            textMesh = GetComponent<TextMeshPro>();

            updateOffset = UnityEngine.Random.Range(0, updateInterval);
        }

        private void Update()
        {
            if ((Time.renderedFrameCount + updateOffset) % updateInterval != 0) return;

            var time = Time.time;
            var position = target.position;
            var speed = Vector3.ProjectOnPlane(position - prevPosition, Vector3.up).magnitude / (time - prevTime) * 1.94384f;
            prevTime = time;
            prevPosition = position;

            var heading = Mathf.RoundToInt(Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(target.forward, Vector3.up), Vector3.up) + 360) % 360;
            textMesh.text = $"{speed:F1}kt\n{(heading == 0 ? 360 : heading)}°";
        }
    }
}
