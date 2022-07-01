using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace USS2
{
    public static class ShipUtility
    {
        public static AnimationCurve ToAnimationCorve(this IEnumerable<(float, float)> keyframes)
        {
            return new AnimationCurve(keyframes.Select(a => new Keyframe(a.Item1, a.Item2)).ToArray());
        }

        public static AnimationCurve TangentSmoothed(this AnimationCurve curve, float weight)
        {
            foreach (var i in Enumerable.Range(0, curve.length)) curve.SmoothTangents(i, weight);
            return curve;
        }
    }
}
