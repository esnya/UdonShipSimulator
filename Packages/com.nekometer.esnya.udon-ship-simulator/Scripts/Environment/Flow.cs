using UdonSharp;
using UnityEngine;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Flow : UdonSharpBehaviour
    {
        /// <summary>
        /// Flow speed by m/s.
        /// </summary>
        public float speed = 2.0f;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            gameObject.layer = 18;
        }

        private void OnDrawGizmosSelected()
        {
            try
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(Vector3.zero, 1.0f);
                Gizmos.DrawRay(Vector3.zero, Vector3.forward * speed);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
#endif
    }
}
