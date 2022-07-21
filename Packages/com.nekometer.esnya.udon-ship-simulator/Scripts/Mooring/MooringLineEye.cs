
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [RequireComponent(typeof(VRCPickup))]
    public class MooringLineEye : UdonSharpBehaviour
    {
        private const int MAX_HIT_COUNT = 64;

        /// <summary>
        /// Hook layers.
        /// </summary>
        public LayerMask hookLayerMask = -1;

        /// <summary>
        /// Is hooked initially.
        /// </summary>
        [SerializeField][FieldChangeCallback(nameof(Hooked))] private bool _hooked;

        [UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(Position))] private Vector3 _position;
        [UdonSynced(UdonSyncMode.Smooth)] private Vector3 velocity;

        private Collider[] hitColliders;
        private VRCPickup pickup;
        private Transform lineRootTransform;
        private Vector3 initialPosition;
        private bool initialHooked;
        private MooringLine line;
        private Vessel vessel;
        private bool initialized;

        public bool Hooked
        {
            get => _hooked;
            set
            {
                _hooked = value;
                if (value)
                {
                    velocity = Vector3.zero;
                    pickup.Drop();
                }
            }
        }

        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                transform.position = value;
            }
        }

        private float seaLevel;

        private void Start()
        {
            Position = initialPosition = transform.position;
            initialHooked = Hooked;
            line = GetComponentInParent<MooringLine>();
            vessel = GetComponentInParent<Vessel>();
        }

        public void _USS_VesselStart()
        {
            hitColliders = new Collider[MAX_HIT_COUNT];

            pickup = (VRCPickup)GetComponent(typeof(VRCPickup));

            lineRootTransform = line.transform;
            seaLevel = vessel.seaLevel;

            Hooked = Hooked;

            initialized = true;
        }

        public void _USS_Respawned()
        {
            Hooked = initialHooked;
            Position = initialPosition;
            velocity = Vector3.zero;
        }

        public override void OnPickup()
        {
            Hooked = false;
        }

        public override void OnPickupUseDown()
        {
            if (Hooked) Hooked = false;
            else
            {
                var hook = GetHook();
                if (!hook) return;

                Hooked = true;
            }

            RequestSerialization();
        }

        private void Update()
        {
            if (!initialized) return;
            if (Networking.IsOwner(gameObject)) Owner_Update();
        }

        private void Owner_Update()
        {
            if (Hooked || pickup.IsHeld)
            {
                velocity = Vector3.zero;
            }
            else
            {
                var deltaTime = Time.deltaTime;

                var p = Position + velocity * deltaTime;
                var lineRootPosition = lineRootTransform.position;
                Position = Vector3.ClampMagnitude(p - lineRootPosition, line.length) + lineRootPosition;

                velocity += ((Position.y > seaLevel ? 1.0f : -1.0f) * Physics.gravity + (transform.position - lineRootPosition) * line.tension) * deltaTime;
            }
        }

        private Transform GetHook()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.01f, hitColliders, hookLayerMask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hitCount; i++)
            {
                var collider = hitColliders[i];
                if (!collider) continue;
                return collider.transform;
            }

            return null;
        }
    }
}
