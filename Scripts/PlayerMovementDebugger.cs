using System.Threading;
using TMPro;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(TextMeshPro))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerMovementDebugger : UdonSharpBehaviour
    {
        private TextMeshPro textMesh;
        private Vector3[]
            position = new Vector3[4],
            rotation = new Vector3[4],
            velocity = new Vector3[4],
            prevPosition = new Vector3[4],
            prevRotation = new Vector3[4],
            prevVelocity = new Vector3[4];

        private void Start()
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        private void FixedUpdate() => UpdatePlayerMovement(0);
        private void Update() => UpdatePlayerMovement(1);
        private void LateUpdate() => UpdatePlayerMovement(2);
        public override void PostLateUpdate() => UpdatePlayerMovement(3);


        private void UpdatePlayerMovement(int index)
        {
            prevPosition[index] = position[index];
            prevRotation[index] = rotation[index];
            prevVelocity[index] = velocity[index];
            position[index] = Networking.LocalPlayer.GetPosition();
            rotation[index] = Networking.LocalPlayer.GetRotation().eulerAngles;
            velocity[index] = Networking.LocalPlayer.GetVelocity();

            if (index != 3) return;

            var deltaTime = Time.deltaTime;
            var text = "Pos\t\tRot\t\tVel\t\tΔPos\t\tΔRot\t\tΔVel\n";
            for (var i = 0; i <= 3; i++)
            {
                float angle, prevAngle;
                Vector3 axis;
                Quaternion.Euler(rotation[i]).ToAngleAxis(out angle, out axis);
                Quaternion.Euler(prevRotation[i]).ToAngleAxis(out prevAngle, out axis);
                text += $"{position[i].magnitude:###0.00}\t{angle:###0.00}\t{velocity[i].magnitude:###0.00}\t\t{Vector3.Distance(position[i], prevPosition[i])/deltaTime:###0.00}\t\t{Mathf.DeltaAngle(angle, prevAngle)/deltaTime:###0.00}\t\t{Vector3.Distance(velocity[i], prevVelocity[i])/deltaTime:###0.00}\n";
            }
            textMesh.text = text;
        }
    }
}
