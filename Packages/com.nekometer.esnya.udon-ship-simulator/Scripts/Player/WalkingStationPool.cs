using System;
using UdonSharp;
using VRC.SDKBase;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WalkingStationPool : UdonSharpBehaviour
    {
        private WalkingStation[] walkingStations;
        [UdonSynced][FieldChangeCallback(nameof(StationActiveFlags))] private uint _stationActiveFlags;
        private uint StationActiveFlags
        {
            get => _stationActiveFlags;
            set {
                _stationActiveFlags = value;

                for (var i = 0; i < walkingStations.Length; i++)
                {
                    var isActive = GetStationActive(i);
                    var station = walkingStations[i];
                    if (station && station.gameObject.activeSelf != isActive)
                    {
                        station.gameObject.SetActive(isActive);
                    }
                }
            }
        }
        private bool GetStationActive(int index)
        {
            return (StationActiveFlags & (1u << index)) != 0;
        }
        private void SetStationActive(int index, bool value)
        {
            _TakeOwnership();
            StationActiveFlags = value ? StationActiveFlags | (1u << index) : StationActiveFlags & ~(1u << index);
            RequestSerialization();
        }

        private void Start()
        {
            walkingStations = GetComponentsInChildren<WalkingStation>(true);
            SendCustomEventDelayedSeconds(nameof(_LateStart), 10);
        }

        public void _LateStart()
        {
            StationActiveFlags = StationActiveFlags;
        }

        public override void Interact()
        {
            _EnterStation();
        }

        public void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _EnterStation()
        {
            var index = GetAvailableIndex();
            if (index < 0) return;

            SetStationActive(index, true);


            var station = walkingStations[index];
            Networking.SetOwner(Networking.LocalPlayer, station.gameObject);
            station.SeatPosition = transform.position;
            station.SeatRotation = transform.rotation.eulerAngles.y;
            station._EnterStation();
        }

        public void _ReturnStation(WalkingStation walkingStation)
        {
            var index = StationIndexOf(walkingStation);
            if (index < 0) return;

            SetStationActive(index, false);
        }

        private int GetAvailableIndex()
        {
            var offset = UnityEngine.Random.Range(0, walkingStations.Length);

            for (var i = 0; i < walkingStations.Length; i++)
            {
                var index = (i + offset) % walkingStations.Length;
                if (!GetStationActive(index)) return index;
            }

            return -1;
        }

        public int StationIndexOf(WalkingStation walkingStation)
        {
            return Array.IndexOf(walkingStations, walkingStation);
        }
    }
}
