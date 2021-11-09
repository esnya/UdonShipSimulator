
using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(Rigidbody))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MovingFloor : UdonSharpBehaviour
    {
        public int bufferSize = 32;
        public float objectSpeedThreshold = 1.0f;
        public float playerExitDelay = 1.0f;
        public float objectExitDelay = 1.0f;
        public float fakeFriction = 0.05f;
        [Popup("@timings")] public int measurementTiming, movingFloorTiming, setPlayerVelocityTiming, teleportPlayerTiming = DISABLED, movingObjectTiming = POST_LATE_UPDATE;

        [NonSerialized] public string[] timings = {
            "Fixed Update",
            "Update",
            "Late Update",
            "Post Late Update",
            "Disabled",
        };

        private bool playerOnFloor;
        private Vector3 prevPosition;
        private Quaternion prevRotation;
        private Rigidbody[] objects;
        private float[] objectExitTimes;
        private Transform parent;
        private Vector3 relativePosition;
        private Quaternion relativeRotation;
        private Rigidbody attachedRigidbody;

        private Vector3 velocity, angularVelocity;

        public const int FIXED_UPDATE = 0;
        public const int UPDATE = 1;
        public const int LATE_UPDATE = 2;
        public const int POST_LATE_UPDATE = 3;
        public const int DISABLED = 4;
        private Vector3 playerMoveInput;
        private float playerExitTime;

        private void Start()
        {
            parent = transform.parent;
            attachedRigidbody = parent.GetComponentInParent<Rigidbody>();
            relativePosition = transform.localPosition;
            relativeRotation = transform.localRotation;
            transform.SetParent(null, true);

            prevPosition = transform.position;
            objects = new Rigidbody[bufferSize];
            objectExitTimes = new float[bufferSize];
        }

        private void FixedUpdate() => UpdateIfNessesory(FIXED_UPDATE, Time.fixedDeltaTime);
        private void Update() => UpdateIfNessesory(UPDATE, Time.deltaTime);
        private void LateUpdate() => UpdateIfNessesory(LATE_UPDATE, Time.deltaTime);
        public override void PostLateUpdate() => UpdateIfNessesory(POST_LATE_UPDATE, Time.deltaTime);

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                playerOnFloor = true;
                playerExitTime = -1;
                Debug.Log($"[MovingFloor] Player Entered");
            }
        }

        public override void OnPlayerTriggerStay(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                playerOnFloor = true;
                playerExitTime = -1;
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                playerExitTime = Time.time;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            EnterObject(other.attachedRigidbody);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            MarkObjectExit(other.attachedRigidbody);
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            playerMoveInput.y = value ? Networking.LocalPlayer.GetJumpImpulse() : 0.0f;
        }

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            playerMoveInput.x = value * Networking.LocalPlayer.GetWalkSpeed();
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            playerMoveInput.z = value * Networking.LocalPlayer.GetWalkSpeed();
        }

        private void UpdateIfNessesory(int timing, float deltaTime)
        {
            if (measurementTiming == timing) UpdateSpeeds();
            if (movingFloorTiming == timing) MoveFloor();
            if (setPlayerVelocityTiming == timing) SetPlayerVelocity();
            if (teleportPlayerTiming == timing) TeleportPlayer(deltaTime);
            if (movingObjectTiming == timing) MoveObjects();

            if (timing == POST_LATE_UPDATE)
            {
                var time = Time.time;
                if (playerOnFloor && playerExitTime > 0  && time - playerExitTime > playerExitDelay)
                {
                    Debug.Log($"[MovingFloor] Player Exited");
                    playerOnFloor = false;
                }

                for (var i = 0; i < bufferSize; i++)
                {
                    if (objects[i] == null) continue;
                    var exitTime = objectExitTimes[i];
                    if (exitTime > 0 && time - exitTime > objectExitDelay)
                    {
                        Debug.Log($"[MovingFloor] Exited: {objects[i].gameObject}");
                        objects[i] = null;
                    }
                }
            }
        }

        private void UpdateSpeeds()
        {
            var position = transform.position;
            var rotation = transform.rotation;
            var positionDiff = position - prevPosition;
            var rotationDiff = rotation * Quaternion.Inverse(prevRotation);
            prevPosition = position;
            prevRotation = rotation;

            var deltaTime = Time.deltaTime;
            velocity = positionDiff / deltaTime;
            angularVelocity = rotationDiff.eulerAngles * deltaTime;
        }

        private void MoveFloor()
        {
            transform.SetPositionAndRotation(
                parent.TransformPoint(relativePosition),
                relativeRotation * parent.rotation
            );
        }

        private void EnterObject(Rigidbody rigidbody)
        {
            if (rigidbody == null || rigidbody == attachedRigidbody) return;

            foreach (var r in objects)
            {
                if (r == rigidbody) return;
            }

            for (var i = 0; i < bufferSize; i++)
            {
                if (objects[i] == null)
                {
                    objects[i] = rigidbody;
                    objectExitTimes[i] = -1;
                    Debug.Log($"[MovingFloor] Entered: {rigidbody.gameObject}");
                    return;
                }
            }
        }

        private void MarkObjectExit(Rigidbody rigidbody)
        {
            if (rigidbody == null) return;

            for (var i = 0; i < bufferSize; i++)
            {
                if (objects[i] == rigidbody)
                {
                    objectExitTimes[i] = Time.time;
                    return;
                }
            }
        }

        private Vector3 GetRotationFactor(Vector3 position, Vector3 center, Vector3 angularVelocity)
        {
            var r1 = position - center;
            var r2 = Quaternion.Euler(angularVelocity) * r1;
            return r2 - r1;
        }
        private void SetPlayerVelocity()
        {
            if (!playerOnFloor) return;

            var localPlayer = Networking.LocalPlayer;
            var playerPosition = localPlayer.GetPosition();

            var velocityFromFloor = Vector3.ProjectOnPlane(velocity, Vector3.up);
            var playerUpVelocity = localPlayer.GetVelocity().y * Vector3.up;
            var movementInputVelocity = localPlayer.GetRotation() * playerMoveInput;

            var rotationFactor = GetRotationFactor(playerPosition, transform.position, angularVelocity);

            localPlayer.SetVelocity(velocityFromFloor + playerUpVelocity + movementInputVelocity + rotationFactor);
        }

        private void TeleportPlayer(float deltaTime)
        {
            if (!playerOnFloor) return;

            Networking.LocalPlayer.TeleportTo(
                Networking.LocalPlayer.GetPosition() + velocity * deltaTime,
                Networking.LocalPlayer.GetRotation() * Quaternion.Euler(angularVelocity * deltaTime)
            );
        }

        private void MoveObjects()
        {
            foreach (var rigidbody in objects)
            {
                if (rigidbody == null || rigidbody.isKinematic) continue;

                var velocityFromFloor = Vector3.ProjectOnPlane(velocity, Vector3.up);
                var velocityUp = Vector3.up * rigidbody.velocity.y;
                var rotationFactor = GetRotationFactor(rigidbody.position, transform.position, angularVelocity);
                var targetVelocity = velocityFromFloor + velocityUp + rotationFactor;

                if (Vector3.Distance(rigidbody.velocity, velocity) > objectSpeedThreshold)
                {
                    rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, targetVelocity, fakeFriction);
                }
                else
                {
                    rigidbody.velocity = targetVelocity;
                }
                rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, angularVelocity, fakeFriction);
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            GetComponent<Rigidbody>().isKinematic = true;
            gameObject.layer = LayerMask.NameToLayer("MirrorReflection");
        }
        #endif
    }
}
