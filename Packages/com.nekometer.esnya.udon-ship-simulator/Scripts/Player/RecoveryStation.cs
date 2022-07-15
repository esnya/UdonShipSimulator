using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{
    [RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RecoveryStation : UdonSharpBehaviour
    {
        private VRC.SDK3.Components.VRCStation station;

        private void Start()
        {
            station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal) station.ExitStation(player);
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
        }

        public void EnterStation()
        {
            station.UseStation(Networking.LocalPlayer);
        }
    }
}
