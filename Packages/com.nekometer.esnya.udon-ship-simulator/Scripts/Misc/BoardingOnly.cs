using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace USS2
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BoardingOnly : UdonSharpBehaviour
    {
        private bool _boarding;
        private bool Boarding
        {
            get => _boarding;
            set {
                _boarding = value;
                gameObject.SetActive(value);
            }
        }

        public void _USS_VesselStart()
        {
            Boarding = Boarding;
        }

        public void _USS_Entered()
        {
            Boarding = true;
        }

        public void _USS_Exited()
        {
            Boarding = false;
        }
    }
}
