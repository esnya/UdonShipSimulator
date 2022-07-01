using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using System;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using UdonSharpEditor;
#endif

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
            station.transform.SetPositionAndRotation(transform.position, transform.rotation);
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

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [Button("Setup Object Pool", true)]
        public void Setup()
        {
            GetComponent<VRCObjectPool>().Pool = this.GetUdonSharpComponentsInChildren<WalkingStation>().Select(s => s.gameObject).ToArray();
        }
#endif
    }
}
