using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    [RequireComponent(typeof(VRC.SDK3.Components.VRCStation))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class WalkingStation : UdonSharpBehaviour
    {
        [Header("Collision Check")]
        /// <summary>
        /// Layer mask for the collision check.
        /// </summary>
        public LayerMask groundLayer = 1 << 29;

        /// <summary>
        /// Maximum height from floor. Exit station If exeeded.
        /// </summary>
        public float maxHeight = 10.0f;

        /// <summary>
        /// Maximum height to climbalbe.
        /// </summary>
        public float stepHeight = 0.8f;

        /// <summary>
        /// Radius of player collider. Half width of miminum hole to pass.
        /// </summary>
        public float playerRadius = 0.2f;

        /// <summary>
        /// Height op player collider. Miminum hole height to pass.
        /// </summary>
        public float playerHeight = 1.6f;

        [Header("Player Moving")]
        /// <summary>
        /// Walk speed.
        /// </summary>
        public float walkSpeed = 1.0f;

        /// <summary>
        /// Rotation speed.
        /// </summary>
        public float rotationSpeed = 200.0f;

        /// <summary>
        /// Delay frames to player fall off the vessel.
        /// </summary>
        public float exitCounter = 10;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(SeatLocalPosition))] private Vector3 _seatLocalPosition;
        private Vector3 SeatLocalPosition
        {
            get => _seatLocalPosition;
            set => _seatLocalPosition = transform.localPosition = value;
        }
        private Vector3 SeatPosition
        {
            get => transform.position;
            set => SeatLocalPosition = transform.parent.InverseTransformPoint(value);
        }

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(SeatLocalRotation))] private float _setLocalRotation;
        private float SeatLocalRotation
        {
            get => _setLocalRotation;
            set => transform.localEulerAngles = Vector3.up * (_setLocalRotation = value % 360.0f);
        }
        private float SeatRotation
        {
            get => SeatLocalRotation + transform.parent.eulerAngles.y;
            set => SeatLocalRotation = value - transform.parent.eulerAngles.y;
        }

        private bool onGround;
        private bool seated;
        private RaycastHit hit;
        private RecoveryStation recoveryStation;
        private Vector3 seatVelocity;
        private VRCStation station;
        private WalkingStationPool pool;
        private int exitCount;

        private void Start()
        {
            station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
            recoveryStation = GetComponentInChildren<RecoveryStation>();
            pool = GetComponentInParent<WalkingStationPool>();
        }

        private void FixedUpdate()
        {
            if (!seated) return;

            var deltaTime = Time.fixedDeltaTime;

            if (onGround || seatVelocity.y <= 0)
            {
                var heightFromFloor = GetHeightFromFloor(SeatPosition);

                if (heightFromFloor <= 0 || heightFromFloor <= Mathf.Clamp(-(seatVelocity.y + Physics.gravity.y) * deltaTime, 0, maxHeight))
                {
                    SeatPosition -= Vector3.up * heightFromFloor;
                    onGround = true;
                    exitCount = 0;
                }
                else
                {
                    onGround = false;
                    if (heightFromFloor <= maxHeight)
                    {
                        exitCount = 0;
                    }
                    else if (exitCount++ >= exitCounter)
                    {
                        _ExitStation();
                    }
                }
            }

            if (!Mathf.Approximately(seatVelocity.magnitude, 0.0f))
            {
                var prevSeatPosition = SeatPosition;
                SeatPosition = GetNextPosition(SeatPosition, seatVelocity, deltaTime);

                if (Mathf.Approximately(Vector3.Distance(SeatPosition, prevSeatPosition), 0.0f)) onGround = true;

                if (onGround)
                {
                    seatVelocity = Vector3.zero;
                }
                else
                {
                    seatVelocity += Physics.gravity * deltaTime;
                }
            }
        }

        private void Update()
        {
            if (!seated) return;

            var moveInput = Vector3.right * Input.GetAxis("Horizontal") + Vector3.forward * Input.GetAxis("Vertical");
            if (moveInput.sqrMagnitude > 0.0f) Move(moveInput * walkSpeed);

            var rotationInput = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") + (Networking.LocalPlayer.IsUserInVR() ? 0.0f : Input.GetAxisRaw("Mouse X"));
            if (!Mathf.Approximately(rotationInput, 0)) Rotate(rotationInput * rotationSpeed);
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                seated = true;
                onGround = true;
                SeatLocalPosition = Vector3.zero;
                SeatLocalRotation = 0.0f;
                exitCount = 0;
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                seated = false;
                if (recoveryStation)
                {
                    recoveryStation.transform.SetPositionAndRotation(SeatPosition, Quaternion.AngleAxis(SeatRotation, Vector3.up));
                    recoveryStation.EnterStation();
                }
                if (pool) pool._ReturnStation(this);

                SeatLocalPosition = Vector3.zero;
                SeatLocalRotation = 0.0f;
            }
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (seated && value) Jump();
        }

        private float GetPlayerSpeed()
        {
            var localPlayer = Networking.LocalPlayer;
            return (localPlayer.IsUserInVR() || Input.GetKey(KeyCode.LeftShift)) ? localPlayer.GetRunSpeed() : localPlayer.GetWalkSpeed();
        }

        private void Move(Vector3 value)
        {
            if (!seated) return;
            var xzVelocity = Vector3.ProjectOnPlane(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * value, Vector3.up).normalized * value.magnitude * GetPlayerSpeed();
            seatVelocity = Vector3.Lerp(seatVelocity, xzVelocity + Vector3.Project(seatVelocity, Vector3.up), onGround ? 1.0f : Time.deltaTime);
        }

        private void Rotate(float speed)
        {
            SeatRotation += speed * Time.deltaTime;
        }

        private void Jump()
        {
            if (!onGround) return;
            onGround = false;
            seatVelocity += Vector3.up * Networking.LocalPlayer.GetJumpImpulse();
        }

        public void _EnterStation()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);
            SeatLocalPosition = Vector3.zero;
            SeatLocalRotation = 0.0f;
            station.UseStation(localPlayer);
        }

        public void _ExitStation()
        {
            station.ExitStation(Networking.LocalPlayer);
        }

        private float GetHeightFromFloor(Vector3 position)
        {
            if (Physics.SphereCast(position + Vector3.up * stepHeight, playerRadius, Vector3.down, out hit, maxHeight + stepHeight, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return position.y - hit.point.y;
            }
            return float.PositiveInfinity;
        }

        private Vector3 GetNextPosition(Vector3 position, Vector3 velocity, float deltaTime)
        {
            var maxDistance = velocity.magnitude * deltaTime;
            if (Physics.CapsuleCast(position + Vector3.up * (stepHeight + playerRadius), position + Vector3.up * (playerHeight - playerRadius), playerRadius, velocity, out hit, maxDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                var diff = Vector3.ClampMagnitude(Vector3.Project(hit.point - position, velocity), maxDistance);
                return position + diff.normalized * Mathf.Clamp01(diff.magnitude - playerRadius);
            }

            return position + velocity * deltaTime;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            this.UpdateProxy();
            if (!seated) return;

            var position = SeatPosition;

            Gizmos.color = onGround ? Color.green : Color.red;
            DrawPlayerCapsule(position);
            if (!onGround)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, Vector3.down * Mathf.Max(GetHeightFromFloor(position), maxHeight));
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, seatVelocity);

            var velocity = Vector3.ProjectOnPlane(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward, Vector3.up).normalized * walkSpeed;
            var deltaTime = 1.0f;
            var maxDistance = velocity.magnitude * deltaTime;

            var nextPosition = GetNextPosition(position, velocity, deltaTime);
            Gizmos.color = Mathf.Approximately(Vector3.Distance(position, nextPosition), maxDistance) ? Color.blue : Color.red;
            DrawPlayerCapsule(nextPosition);

            var heightFromFloor = GetHeightFromFloor(nextPosition);
            if (heightFromFloor <= stepHeight)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(nextPosition, Vector3.down * heightFromFloor);
            }
            else if (heightFromFloor <= maxHeight)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(nextPosition, Vector3.down * heightFromFloor);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(nextPosition, Vector3.down * maxHeight);
            }
        }

        private void DrawPlayerCapsule(Vector3 position)
        {
            Gizmos.DrawWireSphere(position + Vector3.up * playerRadius, playerRadius);
            Gizmos.DrawWireSphere(position + Vector3.up * (playerHeight - playerRadius), playerRadius);
            Gizmos.DrawLine(position + Vector3.up * stepHeight, position + Vector3.up * playerHeight);
        }
#endif
    }
}
