
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MultiFollower : UdonSharpBehaviour
    {
        public Transform sourceContainer;
        public Transform targetContainer;
        public Vector3 positionScale = Vector3.one;
        public bool ownerOnly;
        public bool rotation = true;


        private int count;
        private Transform[] sources, targets;

        private Transform[] GetChildren(Transform parent)
        {
            var count = parent.childCount;
            var children = new Transform[count];
            for (int i = 0; i < count; i++) children[i] = parent.GetChild(i);
            return children;
        }

        private void Start()
        {
            sources = GetChildren(sourceContainer);
            targets = GetChildren(targetContainer);

            count = Mathf.Min(sources.Length, targets.Length);
        }

        private void Update()
        {
            for (int i = 0; i < count; i++)
            {
                var source = sources[i];
                if (ownerOnly && !Networking.IsOwner(source.gameObject)) continue;

                var target = targets[i];

                target.localPosition = Vector3.Scale(source.localPosition, positionScale);
                if (rotation) target.localRotation = source.localRotation;
            }
        }
    }
}
