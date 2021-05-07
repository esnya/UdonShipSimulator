
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(VRCPickup)), RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(SphereCollider))]
    public class TurretController : UdonSharpBehaviour
    {
        public Transform handlePivod;
        public float handleMaxAngle = 45.0f;
        public float handleDeadAngle = 5.0f;

        public Vector3 azimuthAxis = Vector3.up, althuraAxis = Vector3.right;
        public float azimuthMax = 170.0f, alturaMin = -15.0f, althuraMax = 65.0f, azimuthSpeed = 3.0f, althuraSpeed = 3.0f;
        public Transform azimuthHinge, althuraHinge;
        public GunController gun;

        private VRCPickup pickup;
        private Vector3 respawnPosition;
        [UdonSynced(UdonSyncMode.Smooth)] private float azimuth = 0.0f, althura = 0.0f;
        private void Start()
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            respawnPosition = handlePivod.InverseTransformPoint(transform.position);
        }

        private float RemapInput(float value)
        {
            if (Mathf.Abs(value) < handleDeadAngle) return 0.0f;

            return Mathf.Clamp((value - Mathf.Sign(value) * handleDeadAngle) / (handleMaxAngle - handleDeadAngle), -1.0f, 1.0f);
        }

        private float GetInput(Vector3 relativePosition, Vector3 localAxis)
        {
            var worldAxis = handlePivod.parent.TransformDirection(localAxis);

            return RemapInput(Vector3.SignedAngle(handlePivod.parent.up, Vector3.ProjectOnPlane(relativePosition, worldAxis), worldAxis));
        }

        private float ApplyAngle(float angle, Transform target, float input, float speed, float min, float max, Vector3 axis)
        {
            var nextAngle = Mathf.Clamp(angle + input * speed, min, max);
            return nextAngle;
        }

        private void Update()
        {
            azimuthHinge.localRotation = Quaternion.AngleAxis(azimuth, azimuthAxis);
            althuraHinge.localRotation = Quaternion.AngleAxis(althura, althuraAxis);

#if !USS_DEBUG || !UNITY_EDITOR
            if (!pickup.IsHeld) return;
#endif
            var relativePosition = (transform.position - handlePivod.position).normalized;

            var azimuthInput = GetInput(relativePosition, Vector3.forward);
            azimuth = ApplyAngle(azimuth, azimuthHinge, azimuthInput, azimuthSpeed, -azimuthMax, azimuthMax, azimuthAxis);

            var althuraInput = GetInput(relativePosition, Vector3.right);
            althura = ApplyAngle(althura, althuraHinge, althuraInput, althuraSpeed, alturaMin, althuraMax, althuraAxis);

            handlePivod.localRotation = Quaternion.AngleAxis(azimuthInput * handleMaxAngle, Vector3.forward) * Quaternion.AngleAxis(althuraInput * handleMaxAngle, Vector3.right);
        }

        public override void OnPickupUseDown()
        {
            gun.Fire();
        }

        public override void OnDrop()
        {
            transform.position = handlePivod.TransformPoint(respawnPosition);
            transform.rotation = handlePivod.rotation;
        }
    }
}
