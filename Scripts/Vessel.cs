using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Vessel : UdonSharpBehaviour
    {
        public const string EVENT_VesselStart = "_USS_VesselStart";
        public const string EVENT_TakeOwnership = "_USS_TakeOwnership";
        public const string EVENT_LoseOwnership = "_USS_LoseOwnership";

        public bool freezeOnStart = true;
        public GameObject ownerOnly;

        private Rigidbody vesselRigidbody;
        private bool _isOwner;
        private UdonSharpBehaviour[] children;
        private float drag;
        private float angularDrag;

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

            if (freezeOnStart)
            {
                drag = vesselRigidbody.drag;
                angularDrag = vesselRigidbody.angularDrag;
                vesselRigidbody.drag = vesselRigidbody.mass;
                vesselRigidbody.angularDrag = vesselRigidbody.mass;
            }

            _isOwner = Networking.IsOwner(gameObject);
            SendCustomEventDelayedSeconds(nameof(_LateStart), 10);
        }

        public void _LateStart()
        {
            children = (UdonSharpBehaviour[])gameObject.GetComponentsInChildren(typeof(UdonBehaviour), true);
            foreach (var child in children)
            {
                if (child) child.SetProgramVariable("vessel", this);
            }

            if (freezeOnStart)
            {
                vesselRigidbody.velocity = Vector3.zero;
                vesselRigidbody.angularVelocity = Vector3.zero;
                vesselRigidbody.drag = drag;
                vesselRigidbody.angularDrag = angularDrag;
            }

            _SendCustomEventToChildren(EVENT_VesselStart);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            IsOwner = player.isLocal;
        }

        public void _SendCustomEventToChildren(string eventName)
        {
            foreach (var child in children)
            {
                if (child) child.SendCustomEvent(eventName);
            }
        }
    }
}
