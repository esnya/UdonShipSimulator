using System;
using UdonSharp;
using UnityEngine;

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonShipRudder : UdonSharpBehaviour
    {
        public float coefficient = 1e-38f;
        public float backwardMultipilier = 0.2f;
        [NonSerialized] public float waterDensity = 0.99997495f;
        [NonSerialized] public float maxForce = float.MaxValue;

        private new Rigidbody rigidbody;
        private Vector3 Lift {
            get {
                var velocity = rigidbody.velocity;
                var localVelocity = rigidbody.transform.InverseTransformVector(velocity);
                var cl = Vector3.Dot(velocity.normalized, transform.right);
                var l = -0.5f * waterDensity * rigidbody.velocity.sqrMagnitude * coefficient * (localVelocity.z >= 0 ? 1.0f : backwardMultipilier) * cl;
                return Vector3.ClampMagnitude(transform.right * l, maxForce);
            }
        }

        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            rigidbody.AddForceAtPosition(Lift, transform.position, ForceMode.Force);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rigidbody == null) rigidbody = GetComponentInParent<Rigidbody>();

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Lift / rigidbody.mass);
        }
#endif
    }
}
