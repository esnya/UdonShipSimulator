
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Canvas))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1000)]
    public class MovingFloorConfigurator : UdonSharpBehaviour
    {
        public MovingFloor movingFloor;
        private Dropdown[] dropdowns;

        private void Start()
        {
            dropdowns = GetComponentsInChildren<Dropdown>();
            _ReadUI();
        }

        public void _ReadUI()
        {
            movingFloor.measurementTiming = dropdowns[0].value;
            movingFloor.movingFloorTiming = dropdowns[1].value;
            movingFloor.setPlayerVelocityTiming = dropdowns[2].value;
            movingFloor.teleportPlayerTiming = dropdowns[3].value;
            movingFloor.movingObjectTiming = dropdowns[4].value;
        }
    }
}
