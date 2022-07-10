using JetBrains.Annotations;
using UdonSharp;
using UdonToolkit;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Renderer))]
    public class RoundIndicator : UdonSharpBehaviour
    {
        /// <summary>
        /// Target udon behaviour.
        /// </summary>
        [NotNull] public UdonSharpBehaviour sourceBehaviour;

        /// <summary>
        /// Variable value of target udon behaviour.
        /// </summary>
        [Popup("programVariable", "@sourceBehaviour")] public string variableName = "value";

        /// <summary>
        /// Indicator transform to rotate.
        /// </summary>
        [NotNull] public Transform indicator;

        /// <summary>
        /// Rotation axis.
        /// </summary>
        public Vector3 axis = Vector3.up;

        /// <summary>
        /// Max rotation angle in degrees.
        /// </summary>
        [Min(0.0f)] public float maxAngle = 240.0f;

        /// <summary>
        /// Max input value from target udon behaviour.
        /// </summary>
        [Min(0.0f)] public float maxValue = 1.0f;

        /// <summary>
        /// Use absolute value.
        /// </summary>
        public bool absolute = true;

        private bool rendered;

        private void Update()
        {
            rendered = false;
        }

        private  void OnWillRenderObject()
        {
            if (rendered) return;

            var value = (float)sourceBehaviour.GetProgramVariable(variableName);
            var absValue = absolute ? Mathf.Abs(value) : value;
            indicator.localRotation = Quaternion.AngleAxis(absValue * maxAngle / maxValue, axis);

            rendered = true;
        }
    }
}
