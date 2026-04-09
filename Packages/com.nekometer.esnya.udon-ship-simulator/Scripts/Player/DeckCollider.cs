using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DeckCollider : UdonSharpBehaviour
    {
        private Rigidbody vesselRigidbody;
        private Transform vesselTransform;

        private Vector3 localPosition;
        private Quaternion localRotation;

        private void Start()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();
            vesselTransform = vesselRigidbody.transform;

            localPosition = vesselTransform.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(vesselTransform.rotation) * transform.rotation;

            gameObject.name = $"{vesselRigidbody.gameObject.name}_{gameObject.name}";
            transform.parent = vesselTransform.parent;
        }

        public override void PostLateUpdate()
        {
            transform.position = vesselTransform.TransformPoint(localPosition);
            transform.rotation = vesselTransform.rotation * localRotation;
        }
    }
}