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
        /// Width of player collider. Miminum hole width to pass.
        /// </summary>
        public float playerWidth = 1.0f;

        /// <summary>
        /// Height op player collider. Miminum hole height to pass.
        /// </summary>
        public float playerHeight = 1.6f;

        [Header("Player Moving")]
        /// <summary>
        /// Walk speed.
        /// </summary>
        public float walkSpeed = 2.0f;

        /// <summary>
        /// Rotation speed.
        /// </summary>
        public float rotationSpeed = 200.0f;

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

        private void Start()
        {
            station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
            recoveryStation = GetComponentInChildren<RecoveryStation>();
            pool = GetComponentInParent<WalkingStationPool>();
        }

        private void FixedUpdate()
        {
            if (!seated) return;

            if (onGround)
            {
                seatVelocity = Vector3.zero;
            }
            else
            {
                if (Physics.Raycast(SeatPosition, Vector3.down, out hit, maxHeight, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    SeatPosition += seatVelocity * Time.fixedDeltaTime;
                    var deltaTime = Time.fixedDeltaTime;
                    seatVelocity += Physics.gravity * deltaTime;

                    Move(seatVelocity);
                }
                else
                {
                    _ExitStation();
                }
            }
        }

        private void Update()
        {
            if (!seated) return;

            var moveInput = Vector3.right * Input.GetAxisRaw("Horizontal") + Vector3.forward * Input.GetAxisRaw("Vertical");
            if (onGround && moveInput.magnitude > 0.0f) Move(moveInput * walkSpeed);

            var rotationInput = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") + (Networking.LocalPlayer.IsUserInVR() ? 0.0f : Input.GetAxisRaw("Mouse X"));
            if (!Mathf.Approximately(rotationInput, 0)) Rotate(rotationInput * rotationSpeed);

            // Debug.Log($"m: {moveInput}, r: {rotationInput} J1A4: {Input.GetAxis("Joy1 Axis 4"):F2}, MX: {Input.GetAxis("Mouse X"):F2}");

            // if (Input.GetButton("Jump")) Jump();
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                seated = true;
                onGround = true;
                SeatLocalPosition = Vector3.zero;
                SeatLocalRotation = 0.0f;
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                seated = false;
                if (recoveryStation) recoveryStation.EnterStation();
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

            var deltaTime = Time.deltaTime;

            var localPlayer = Networking.LocalPlayer;
            var diffVector = Quaternion.FromToRotation(Vector3.forward, Vector3.ProjectOnPlane(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward, Vector3.up)) * value * (GetPlayerSpeed() * deltaTime);
            var nextPosition = SeatPosition + diffVector;

            var collision = Physics.SphereCast(nextPosition + Vector3.up * (playerHeight - playerWidth * 0.5f), playerWidth * 0.5f, Vector3.down, out hit, playerHeight - playerWidth + stepHeight, groundLayer, QueryTriggerInteraction.Ignore);
            var ydiff = collision ? hit.point.y - nextPosition.y : -maxHeight;
            var canMove = !collision || ydiff < stepHeight;
            onGround = ydiff > -stepHeight;

            if (canMove) SeatPosition = nextPosition + Vector3.up * Mathf.Max(ydiff, 0.0f);

            // Debug.Log($"Movew: onGround: {onGround}, canMove: {canMove}, ydiff: {ydiff:F2},  hit.distance: {hit.distance:F2}, hit.point: {hit.point}, hit.normal: {hit.normal}");
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

#if !COMPILER_UDONSHARP && UNITY_EDITOR
#endif
    }
}
