using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MooringLine : UdonSharpBehaviour
    {
        /// <summary>
        /// Maximum load in N.
        // </summary>

        // [Min(0.0f)] public float breakingLoad = 60000.0f;

        /// <summary>
        /// Length in meters.
        /// </summary>
        public float length = 200.0f;

        /// <summary>
        /// Tip of line. Such as MooringLineEye.
        /// </summary>
        [NotNull] public Rigidbody tip;

        /// <summary>
        /// Root of line. GetFromParent if null.
        /// </summary>
        [CanBeNull] public Rigidbody root;

        private LineRenderer debugLineRenderer;

        private void Start()
        {
            if (!root)
            {
                root = GetComponentInParent<Rigidbody>();
            }

            debugLineRenderer = GetComponentInChildren<LineRenderer>();
        }
    }
}
