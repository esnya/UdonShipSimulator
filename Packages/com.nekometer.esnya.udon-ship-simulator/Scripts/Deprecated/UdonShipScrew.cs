using UdonSharp;
using UnityEngine;
#if !COMPILER_UDONSHARP && UNITY_EDITOR

using UdonSharpEditor;
#endif

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonShipScrew : UdonSharpBehaviour
    {
        [Range(-1.0f, 1.0f)] public float throttle = 0.0f;
        // [Range(0.0f, 1.0f)] public float efficiency = 0.8f;
        // public float enginePower = 10000;
        public float thrust = 10000;

        private new Rigidbody rigidbody;

        // private Vector3 Thrust => transform.forward * (throttle * 75.0f * efficiency * enginePower / Mathf.Max(Mathf.Sign(throttle) * Vector3.Dot(rigidbody.velocity, transform.forward), 1.0f));
        private Vector3 Thrust => transform.forward * throttle * thrust;

        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            rigidbody.AddForceAtPosition(Thrust, transform.position, ForceMode.Force);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rigidbody == null) rigidbody = GetComponentInParent<Rigidbody>();

            this.UpdateProxy();
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Thrust / rigidbody.mass);
        }
#endif
    }
}
