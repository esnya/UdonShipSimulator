
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(VRCPickup))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HelmController : UdonSharpBehaviour
    {
        public Transform hinge;
        [Tooltip("Local Space")] public Vector3 hingeAxis = Vector3.forward;
        [ListView("Rotation Targets")] public Transform[] rotationTargets = { null };
        [ListView("Rotation Targets"), Tooltip("Local Space")] public Vector3[] rotationAxies = { Vector3.up };
        // [ListView("Rotation Targets"), Tooltip("Deggree/s")] public float[] speeds = {1.0f};
        float[] targetAngles;
        [Range(0, 90)] public float maxAngle = 35.0f;
        public float angleRatio = 32f;
        private VRCPickup pickup;
        private int targetCount;
        public bool debug;
        /*[UdonSynced(UdonSyncMode.Smooth)]*/ private float angle = 0.0f;
        private void Start()
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));

            targetCount = Mathf.Min(rotationTargets.Length, rotationAxies.Length);
            targetAngles = new float[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                targetAngles[i] = Vector3.Dot(rotationTargets[i].localRotation.eulerAngles, rotationAxies[i]);
            }
        }

        private bool isFirst;
        private Vector3 prevPosition;
        private float prevAngle = 0.0f;
        private void Update()
        {
            if (pickup.IsHeld || debug)
            {
                var worldAxis = hinge.TransformDirection(hingeAxis);
                var position = Vector3.ProjectOnPlane((transform.position - hinge.position).normalized, worldAxis).normalized;

                if (!isFirst)
                {
                    var movedAngle = Vector3.SignedAngle(prevPosition, position, worldAxis);
                    if (movedAngle != 0)
                    {
                        angle = Mathf.Clamp(angle + movedAngle, -maxAngle * angleRatio, maxAngle * angleRatio);
                    }
                }

                isFirst = false;
                prevPosition = position;
            }

            if (angle != prevAngle) Apply();
            prevAngle = angle;
        }

        public override void OnDrop()
        {
            transform.position = hinge.position;
            transform.rotation = hinge.rotation;
            isFirst = true;
        }

        private void Apply()
        {
            hinge.localRotation = Quaternion.AngleAxis(angle, hingeAxis);

            var scaledAngle = angle / angleRatio;
            for (int i = 0; i < targetCount; i++)
            {
                targetAngles[i] = scaledAngle;
                rotationTargets[i].localRotation = Quaternion.AngleAxis(targetAngles[i], rotationAxies[i]);
            }
        }
    }
}
