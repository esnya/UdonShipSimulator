using System;
using UdonSharp;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace USS2
{
    /// <summary>
    /// Descrive static hull shape.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HullProfile : UdonSharpBehaviour
    {
        /// <summary>
        /// Curve shows keel depth from upper deck by side view. (0 to 1 means bow side edge of upper deck to stern side, 0 to -1 means upperdeck to keel on midship)
        /// </summary>
        public AnimationCurve profile = AnimationCurve.Constant(0.0f, 1.0f, -1.0f);

        /// <summary>
        /// Curve shows breadth of hull by top view. (0 to 1 means bow side edge of upper deck to stern side, 0 to 1 means center of hull to beam on midship)
        /// </summary>
        public AnimationCurve halfBreadthPlan = AnimationCurve.Linear(0.0f, 0.0f, 0.5f, 1.0f);

        /// <summary>
        /// Curve shows depth from upperdeck to keel on midship by front view. (0 to 1 means center of hull to beam, 0 to -1 means upperdec to keel on midship)
        /// </summary>
        public AnimationCurve bodyPlan = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// Length in meters on upperdeck
        /// </summary>
        public float length = 308.0f;

        /// <summary>
        /// Maximum breadth in meters on midship.
        /// </summary>
        public float beam = 38.0f;

        /// <summary>
        /// Maximum depth in meters on midship. Sum of draught and freeboard.
        /// </summary>
        public float depth = 16.0f;

        /// <summary>
        /// Draught in meters to estimate kinematic parameters.
        /// </summary>
        public float designedDraught = 13.0f;

        /// <summary>
        /// Sampling steps used on _UpdateParameters to calculate some parameters..
        /// </summary>
        [Range(2, 128)] public int curveSamplingCount = 32;

        /// <summary>
        /// Waterline length in meters on designed draught.
        /// </summary>
        [NonSerialized] float waterlineLength;

        /// <sumamry>
        /// Midship area in squared meters.
        /// </summary>
        [NonSerialized] public float midshipSectionArea;

        /// <summary>
        /// Waterplane area in squared meters.
        /// </summary>
        [NonSerialized] public float waterplaneArea;

        /// <summary>
        /// Volume under water.
        /// </summary>
        [NonSerialized] public float volume;

        /// <summary>
        /// Midship section coefficient.
        /// </summary>
        [NonSerialized] public float cm;

        /// <summary>
        /// Warterplane area coefficient.
        /// </summary>
        [NonSerialized] public float cw;

        /// <summary>
        /// Block coefficient.
        /// </summary>
        [NonSerialized] public float cb;

        /// <summary>
        /// Prismatic coefficient.
        /// </summary>
        [NonSerialized] public float cp;

        /// <summary>
        /// Vertical prismatic coefficient.
        /// </summary>
        [NonSerialized] public float cvp;

        private void Start()
        {
            _UpdateParameters();
            enabled = false;
        }

        /// <summary>
        /// Estiamte and store parameters.
        /// </summary>
        public void _UpdateParameters()
        {
            var freeboard = depth - designedDraught;
            var dx = 1.0f / curveSamplingCount * length;
            var dy = 1.0f / curveSamplingCount * beam * 0.5f;

            midshipSectionArea = 0.0f;
            for (var i = 0; i < curveSamplingCount; i++)
            {
                var y = (i + 0.5f) * dy;
                var dd = Mathf.Max(GetMidshipDepthAt(y) - freeboard, 0.0f);
                midshipSectionArea += dd * dy;
            }
            midshipSectionArea *= 2.0f;

            var midshipBreathByDeth = CreateAnimationCurve();
            for (var i = 0; i <= curveSamplingCount; i++)
            {
                var y = i * dy;
                midshipBreathByDeth.AddKey(GetMidshipDepthAt(y), y);
            }
            SmoothCurveTangent(midshipBreathByDeth, 1.0f);

            waterplaneArea = 0.0f;
            for (var i = 0; i <= curveSamplingCount; i++)
            {
                var x = i * dx;
                waterplaneArea += midshipBreathByDeth.Evaluate(freeboard) * GetBeamAt(x) / beam * dx;
            }

            var forwardKeelXByDepth = CreateAnimationCurve();
            var afterKeelXByDepth = CreateAnimationCurve();
            for (var i = 0; i < curveSamplingCount; i++)
            {
                var x = (i + 0.5f) * dx * 0.5f;
                forwardKeelXByDepth.AddKey(GetKeelDepthAt(x), x);
                afterKeelXByDepth.AddKey(GetKeelDepthAt(length - x), x);
            }
            SmoothCurveTangent(forwardKeelXByDepth, 1.0f);
            SmoothCurveTangent(afterKeelXByDepth, 1.0f);

            volume = 0.0f;
            for (var i = 0; i < curveSamplingCount; i++)
            {
                var x = (i + 0.5f) * dx;
                volume += midshipSectionArea * GetBeamAt(x) / beam;
            }

            waterlineLength = forwardKeelXByDepth.Evaluate(freeboard) + afterKeelXByDepth.Evaluate(freeboard);

            cm = midshipSectionArea / (beam * designedDraught);
            cw = waterplaneArea / (beam * waterlineLength);
            cb = volume / (beam * designedDraught * waterlineLength);
            cp = volume / (midshipSectionArea * waterlineLength);
            cvp = volume / (waterplaneArea * designedDraught);
        }

        public AnimationCurve CreateAnimationCurve()
        {
            var curve = AnimationCurve.Constant(0.0f, 0.0f, 0.0f);
            while (curve.length > 0) curve.RemoveKey(0);
            return curve;
        }

        public void SmoothCurveTangent(AnimationCurve curve, float weight)
        {
            for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, weight);
        }

        public float GetBeamAt(float x)
        {
            return halfBreadthPlan.Evaluate(x / length) * beam;
        }

        public float GetKeelDepthAt(float x)
        {
            return -profile.Evaluate(x / length) * depth;
        }

        public float GetMidshipDepthAt(float y)
        {
            return -bodyPlan.Evaluate(y / beam * 0.5f) * depth;
        }

        public float GetDepthAt(float x, float y)
        {
            return profile.Evaluate(x / length) * bodyPlan.Evaluate(y / beam * 0.5f) * depth;
        }
    }
}
