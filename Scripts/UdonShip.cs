using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Rigidbody))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(1000)] // After UdonShipHull
    public class UdonShip : UdonSharpBehaviour
    {
        public float respawnFreezingTime = 1.0f;
        public float maxAcceleration = 18.0f;
        public float waterDensity = 0.99997495f;
        public float waterViscosity = 0.000890f;

        private UdonShipHull hull;
        private new Rigidbody rigidbody;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Start()
        {
            hull = GetComponentInChildren<UdonShipHull>();
            rigidbody = GetComponent<Rigidbody>();

            initialPosition = transform.position;
            initialRotation = transform.rotation;

            var seaHeight = transform.parent.position.y;
            var maxForce = rigidbody.mass * maxAcceleration;

            hull.seaHeight = seaHeight;
            hull.maxForce = maxForce;
            hull.waterDensity = waterDensity;
            hull.waterViscosity = waterViscosity;

            foreach (var rudder in GetComponentsInChildren<UdonShipRudder>())
            {
                rudder.waterDensity = waterDensity;
                rudder.maxForce = maxForce;
            }

            if (Networking.IsMaster) _Respawn();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            hull.gameObject.SetActive(player.isLocal);
        }

        public void _Respawn()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            hull.gameObject.SetActive(false);

            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            SendCustomEventDelayedSeconds(nameof(_Respawned), respawnFreezingTime);
        }

        public void _Respawned()
        {
            rigidbody.isKinematic = false;
            hull.gameObject.SetActive(Networking.IsOwner(gameObject));
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.isKinematic = true;
        }
#endif
    }
}
