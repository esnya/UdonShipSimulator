
using UdonSharp;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(VRCPickup)), RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(SphereCollider))]
    public class LeverController : UdonSharpBehaviour
    {
        public Transform hinge;
        public Vector3 hingeAxis = Vector3.right;
        public Vector3 hingeUp = Vector3.up;
        [Range(-360, 360)] public float maxAngle = 45.0f;
        public float[] snapAngles = { 0.0f };
        public float snapDistance = 5.0f;
        public bool inverse = true;

        public int thrusterIndex = 0;
        public UdonShip ship;

        private VRCPickup pickup;
        private Vector3 respawnPosition;
        private void Start()
        {
            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            respawnPosition = hinge.InverseTransformPoint(transform.position);
        }

        private void Update()
        {
#if !USS_DEBUG || !UNITY_EDITOR
            if (!pickup.IsHeld) return;
#endif

            var worldAxis = hinge.TransformDirection(hingeAxis);
            var worldUp = hinge.parent.TransformDirection(hingeUp);
            var position = Vector3.ProjectOnPlane(transform.position - hinge.position, worldAxis);
            var angle = Mathf.Clamp(Vector3.SignedAngle(worldUp, position, worldAxis), -maxAngle, maxAngle);
            var absAngle = Mathf.Abs(angle);
            var angleSign = Mathf.Sign(angle);

            foreach (var snapAngle in snapAngles)
            {
                if (Mathf.Abs(absAngle - snapAngle) <= snapDistance) angle = snapAngle * angleSign;
            }

            hinge.localRotation = Quaternion.Euler(angle, 0, 0);
            var power = Mathf.Clamp(angle / maxAngle, -1, 1);
            if (inverse) power *= -1;
            if (ship != null && Networking.IsOwner(ship.gameObject))
            {
                ship.SetThrustPower(power);
            }
        }

        public override void OnDrop()
        {
            transform.position = hinge.TransformPoint(respawnPosition);
            transform.rotation = hinge.rotation;
        }
    }
}
