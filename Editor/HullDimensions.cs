using UnityEngine;
/*
namespace USS
{
    public class HullDimensions
    {
        public static float GetCB(this Hull hull)
        {
            return hull.GetVolume(hull.depth) / (hull.beam * hull.length * hull.depth);
        }
        public static float GetK(this Hull hull, float l, float b, float d)
        {
            return 0.0905f + 1.51f * hull.GetCB() / (Mathf.Pow(hull.l/hull.b, 2.0f) * Mathf.Sqrt(hull.b / hull.d));
        }

        public static Vector3 GetFormFactor(this Hull hull)
        {
            var cb = hull.GetCB();
            return Vector3.one + new Vector3(GetK(cb, hull.beam, hull.length, hull.depth), hull,GetK(cb, hull.depth, hull.beam, hull.depth), hull,GetK(cb, hull.length, hull.beam, hull.depth));
        }

        public static int GetBlockIndex(this Hull hull, int lengthIndex, int beamIndex)
        {
            return (lengthIndex * beamSteps + beamIndex) * 2;
        }

        public static float GetKeelDepthAt(this Hull hull, float v)
        {
            return -sheerProfile.Evaluate(v) * depth;
        }

        public static float GetDepthAt(this Hull hull, float u, float v)
        {
            return GetKeelDepthAt(v) * -bodyProfile.Evaluate(u);
        }

        public static float GetBeamAt(this Hull hull, float v)
        {
            return halfBreadthProfile.Evaluate(v) * beam;
        }

        public static Vector3 GetBlockCenterOfBuoyancy(this Hull hull, int index)
        {
            return transform.TransformPoint(blockBottomPoints[index] + Vector3.up * draughts[index] * 0.5f);
        }

        public static Vector3 GetBlockBuoyancy(this Hull hull, int index, Vector3 gravity)
        {
            return -gravity * buoyancies[index];
        }

        public static float GetBlockLength(this Hull hull)
        {
            return length / lengthSteps;
        }
        public static float GetBlockWidth(this Hull hull, float v)
        {
            return GetBeamAt(v) * 0.5f / beamSteps;
        }

        public static float GetBlockCrossSectionArea(this Hull hull, float u, float v)
        {
            var b = GetBeamAt(v) * u;
            var d = GetDepthAt(u, v);
            return b * d;
        }

        public static float GetDraughtAt(this Hull hull, float u, float v, float draught)
        {
            return DepthToDraught(GetDepthAt(u, v), draught);
        }

        public static float DepthToDraught(this Hull hull, float d, float draught)
        {
            return Mathf.Clamp(draught - depth + d, 0.0f, d);
        }

        public static float GetVolume(this Hull hull, float draught)
        {
            var volume = 0.0f;
            var l = hull.length / hull.lengthSteps;

            for (var i = 0; i < hull.lengthSteps; i++)
            {
                var v = (i + 0.5f) / hull.lengthSteps;
                volume += GetCrossSectionAreaByDraughtProfile(v).Evaluate(draught) * l;
            }

            return volume;
        }

        public static AnimationCurve CreateAnimationCureve(this Hull hull)
        {
            return new AnimationCurve();
        }

        public static void CurveSmoothTangents(this Hull hull, AnimationCurve curve, float weight)
        {
            for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, weight);
        }

        public static AnimationCurve GetCrossSectionBodyProfileAt(this Hull hull, float v)
        {
            var result = CreateAnimationCureve();

            for (var i = 0; i <= hull.curveProfilingSteps; i++)
            {
                var u = (float)i / hull.curveProfilingSteps;
                result.AddKey(u, GetBeamAt(v) * u);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetCrossSectionDepthProfileAt(this Hull hull, float v)
        {
            var result = CreateAnimationCureve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = (float)i / curveProfilingSteps;
                result.AddKey(u, GetDepthAt(u, v));
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetCrossSectionAreaByDraughtProfile(this Hull hull, float v)
        {
            var result = CreateAnimationCureve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var draught = (float)i / curveProfilingSteps * depth;

                var crossSectionArea = 0.0f;
                var w = GetBeamAt(v) / curveProfilingSteps;
                for (var j = 0; j < curveProfilingSteps; j++)
                {
                    var u = (j + 0.5f) / curveProfilingSteps;
                    var h = GetDraughtAt(u, v, draught);
                    crossSectionArea += h * w;
                }
                result.AddKey(draught, crossSectionArea);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetCrossSectionArcByDraughtProfileAt(this Hull hull, float v)
        {
            var segments = CreateAnimationCureve();
            for (var j = 0; j < curveProfilingSteps; j++)
            {
                var u0 = (j + 0.5f) / curveProfilingSteps;
                var u1 = (j + 0.0f) / curveProfilingSteps;
                var u2 = (j + 1.0f) / curveProfilingSteps;

                var p1 = new Vector2(GetBeamAt(v) * u1 * 0.5f, GetDepthAt(u1, v));
                var p2 = new Vector2(GetBeamAt(v) * u2 * 0.5f, GetDepthAt(u2, v));

                var l = Vector2.Distance(p1, p2);
                segments.AddKey(depth - GetDepthAt(u0, v), l);
            }
            CurveSmoothTangents(segments, 1.0f);

            var result = CreateAnimationCureve();
            var length = 0.0f;
            result.AddKey(0.0f, 0.0f);
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var draught = (float)(i + 0.5f) / curveProfilingSteps * depth;
                length += segments.Evaluate(draught);
                result.AddKey(draught, length);
            }
            CurveSmoothTangents(result, 1.0f);
            return result;
        }

        public static AnimationCurve GetCrossSectionBeamByDraughtProfileAt(this Hull hull, float v)
        {
            var result = CreateAnimationCureve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = Mathf.Pow((float)i / curveProfilingSteps, beamDraughtSamplingCurve);
                var b = GetBeamAt(v) * u;
                var d = depth - GetDepthAt(u, v);
                result.AddKey(d, b);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetVolumeByDraughtProfile(this Hull hull)
        {
            var result = CreateAnimationCureve();

            var dl = length / curveProfilingSteps;

            var areaProfiles = new AnimationCurve[curveProfilingSteps];
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var v = (i + 0.5f) / curveProfilingSteps;
                areaProfiles[i] = GetCrossSectionAreaByDraughtProfile(v);
            }

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var draught = (float)i / curveProfilingSteps * depth;

                var volume = 0.0f;
                for (var j = 0; j < curveProfilingSteps; j++)
                {
                    volume += areaProfiles[j].Evaluate(draught) * dl;
                }
                result.AddKey(draught, volume);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetSurfaceAreaByDraughtProfile(this Hull hull)
        {
            var result = CreateAnimationCureve();

            var dl = length / curveProfilingSteps;

            var arcProfiles = new AnimationCurve[curveProfilingSteps];
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var v = (i + 0.5f) / curveProfilingSteps;
                arcProfiles[i] = GetCrossSectionArcByDraughtProfileAt(v);
            }

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var draught = (float)i / curveProfilingSteps * depth;

                var area = 0.0f;
                for (var j = 0; j < curveProfilingSteps; j++)
                {
                    area += arcProfiles[j].Evaluate(draught) * dl;
                }
                result.AddKey(draught, area);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetWaterplaneAreaByDraughtProfile(this Hull hull)
        {
            var result = CreateAnimationCureve();

            var dl = length / curveProfilingSteps;

            var beamProfiles = new AnimationCurve[curveProfilingSteps];
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var v = (i + 0.5f) / curveProfilingSteps;
                beamProfiles[i] = GetCrossSectionBeamByDraughtProfileAt(v);
            }

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var draught = (float)i / curveProfilingSteps * depth;

                var area = 0.0f;
                for (var j = 0; j < curveProfilingSteps; j++)
                {
                    area += beamProfiles[j].Evaluate(draught) * dl;
                }
                result.AddKey(draught, area);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetBeamByDraughtProfile(this Hull hull)
        {
            var result = CreateAnimationCureve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = Mathf.Pow((float)i / curveProfilingSteps, beamDraughtSamplingCurve);
                var draught = depth * (1 + bodyProfile.Evaluate(u));
                result.AddKey(draught, u * beam);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetWaterLineLengthByDraughtProfile(this Hull hull)
        {
            var bowSide = CreateAnimationCureve();
            var sternSide = CreateAnimationCureve();

            var steps = curveProfilingSteps;

            for (var i = 0; i < steps; i++)
            {
                var vb = Mathf.Pow((i + 0.5f) / steps, 1.0f / beamDraughtSamplingCurve) * 0.5f;
                var vs = 1.0f - vb;

                var db = depth - GetKeelDepthAt(vb);
                var ds = depth - GetKeelDepthAt(vs);
                var l = (0.5f - vb) * length;

                if (db > 0) bowSide.AddKey(db, l);
                if (ds > 0) sternSide.AddKey(ds, l);
            }
            bowSide.AddKey(depth, length / 2.0f);
            sternSide.AddKey(depth, length / 2.0f);
            CurveSmoothTangents(bowSide, 1.0f);
            CurveSmoothTangents(sternSide, 1.0f);

            var result = CreateAnimationCureve();
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var draught = (i + 0.5f) / curveProfilingSteps * depth;

                var lb = bowSide.Evaluate(draught);
                var ls = sternSide.Evaluate(draught);
                if (lb < length / 2 && ls < length / 2)
                {
                    var l = lb + ls;
                    result.AddKey(draught, l);
                }
            }
            result.AddKey(depth, length);
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public static AnimationCurve GetBlockVolumeProfile(this Hull hull, float u, float v)
        {
            var result = CreateAnimationCureve();

            var w = GetBlockWidth(v);
            var l = GetBlockLength();
            var horizontalArea = w * l;
            var d = GetDepthAt(u, v);

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var draught = (float)i / curveProfilingSteps * depth;
                var h = Mathf.Clamp(draught, 0.0f, d);

                var volume = horizontalArea * h;

                result.AddKey(draught, volume);
            }
            CurveSmoothTangents(result, 0.0f);

            return result;
        }

        public static AnimationCurve GetBlockSurfaceProfile(this Hull hull, float u, float v)
        {
            var result = CreateAnimationCureve();

            var l = GetBlockLength();
            var a = 0.0f;

            for (var i = 0; i < hull.curveProfilingSteps; i++)
            {
                var u1 = u + Mathf.Max((float)i / hull.curveProfilingSteps - 0.5f) / hull.beamSteps;
                var u2 = u + Mathf.Max((float)(i + 1) / hull.curveProfilingSteps - 0.5f) / hull.beamSteps;

                var d1 = GetDepthAt(u1, v);
                var d2 = GetDepthAt(u2, v);

                var w = Vector2.Distance(new Vector2(u1, d1), new Vector2(u2, d2));
                a += w * l;

                result.AddKey(hull.depth - (d1 + d2) / 2.0f, a);
            }

            CurveSmoothTangents(result, 0.0f);

            return result;
        }
    }
}
*/