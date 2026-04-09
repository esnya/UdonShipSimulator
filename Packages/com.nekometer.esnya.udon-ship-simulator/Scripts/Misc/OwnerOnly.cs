using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OwnerOnly : UdonSharpBehaviour
    {
        [NonSerialized] public Vessel vessel;

        public void _USS_VesselStart()
        {
            if (!Networking.IsOwner(vessel.gameObject)) _USS_LoseOwnership();
        }

        public void _USS_TakeOwnership()
        {
            gameObject.SetActive(true);
        }

        public void _USS_LoseOwnership()
        {
            gameObject.SetActive(false);
        }
    }
}
