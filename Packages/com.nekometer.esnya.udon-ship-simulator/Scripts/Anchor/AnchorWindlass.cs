using System;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AnchorWindlass : UdonSharpBehaviour
    {
        /// <summary>
        /// Speed in m/s.
        /// </summary>
        public float speed = 1.0f;

        /// <summary>
        /// Input speed.
        /// </sumamry>
        [NonSerialized] public float targetSpeed = 0.0f;
    }
}
