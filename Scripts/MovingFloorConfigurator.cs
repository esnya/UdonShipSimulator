
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Canvas))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(1000)]
    public class MovingFloorConfigurator : UdonSharpBehaviour
    {
        public MovingFloor movingFloor;
        private Dropdown[] dropdowns;
        private Slider[] sliders;

        private void Start()
        {
            dropdowns = GetComponentsInChildren<Dropdown>();
            sliders = GetComponentsInChildren<Slider>();
            _ReadUI();
        }

        public void _ReadUI()
        {
            movingFloor.measurementTiming = dropdowns[0].value;
            movingFloor.movingFloorTiming = dropdowns[1].value;
            movingFloor.setPlayerVelocityTiming = dropdowns[2].value;
            movingFloor.teleportPlayerTiming = dropdowns[3].value;
            movingFloor.movingObjectTiming = dropdowns[4].value;

            movingFloor.velocitySmoothing = sliders[0].value;
            movingFloor.angularVelocitySmoothing = sliders[1].value;
            movingFloor.fakeFriction = sliders[2].value;
        }
    }
}
