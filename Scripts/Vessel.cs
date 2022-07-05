using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(300)] // After Hull
    public class Vessel : UdonSharpBehaviour
    {
        public const string EVENT_VesselStart = "_USS_VesselStart";
        public const string EVENT_TakeOwnership = "_USS_TakeOwnership";
        public const string EVENT_LoseOwnership = "_USS_LoseOwnership";
        public const string EVENT_Respawned = "_USS_Respawned";

        public bool freezeOnStart = true;
        public GameObject ownerOnly;

        private Rigidbody vesselRigidbody;
        private bool _isOwner;
        private UdonSharpBehaviour[] children;
        private float drag;
        private float angularDrag;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private VRCObjectSync objectSync;
        private object seaLevel;

        public bool IsOwner
        {
            get => _isOwner;
            private set
            {
                _isOwner = value;
                _SendCustomEventToChildren(value ? EVENT_TakeOwnership : EVENT_LoseOwnership);
                if (ownerOnly) ownerOnly.SetActive(value);
            }
        }

        private void Start()
        {
            vesselRigidbody = GetComponent<Rigidbody>();
            objectSync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));

            var ocean = GetComponentInParent<Ocean>();
            if (ocean)
            {
                seaLevel = ocean.transform.position.y;
            }

            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            drag = vesselRigidbody.drag;
            angularDrag = vesselRigidbody.angularDrag;

            if (freezeOnStart) Freeze();

            IsOwner = Networking.IsOwner(gameObject);
            SendCustomEventDelayedSeconds(nameof(_LateStart), 10);
        }

        public void _LateStart()
        {
            children = (UdonSharpBehaviour[])gameObject.GetComponentsInChildren(typeof(UdonBehaviour), true);
            foreach (var child in children)
            {
                if (child) child.SetProgramVariable("vessel", this);
            }

            _SendCustomEventToChildren(EVENT_VesselStart);
        }

        private void Freeze()
        {
            vesselRigidbody.drag = vesselRigidbody.mass;
            vesselRigidbody.angularDrag = vesselRigidbody.mass;
            SendCustomEventDelayedSeconds(nameof(_Unfreeze), 10);
        }

        public void _Unfreeze()
        {
            vesselRigidbody.velocity = Vector3.zero;
            vesselRigidbody.angularVelocity = Vector3.zero;
            vesselRigidbody.drag = drag;
            vesselRigidbody.angularDrag = angularDrag;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (IsOwner = player.isLocal)
            {
                foreach (var child in children)
                {
                    if (child && !Networking.IsOwner(child.gameObject)) Networking.SetOwner(player, child.gameObject);
                }
            }
        }

        public void _SendCustomEventToChildren(string eventName)
        {
            if (children == null) return;
            foreach (var child in children)
            {
                if (child) child.SendCustomEvent(eventName);
            }
        }

        public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        public void _Respawn()
        {
            _TakeOwnership();

            Freeze();
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
            if (objectSync) objectSync.FlagDiscontinuity();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnRespawned));
        }

        public void OnRespawned() => _SendCustomEventToChildren(EVENT_Respawned);
    }
}
