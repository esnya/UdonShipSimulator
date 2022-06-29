using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Vessel : UdonSharpBehaviour
    {
        public GameObject ownerOnly;

        private bool _isOwner;
        public bool IsOwner
        {
            get => _isOwner;
            private set {
                _isOwner = value;
                if (ownerOnly) ownerOnly.SetActive(ownerOnly);
            }
        }

        private void Start()
        {
            IsOwner = Networking.IsOwner(gameObject);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            IsOwner = player.isLocal;
        }
    }
}
