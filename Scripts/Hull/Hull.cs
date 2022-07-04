using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;
using UdonShipSimulator;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace USS2
{
    [DefaultExecutionOrder(200)] // After Appendages
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Hull : UdonSharpBehaviour
    {
        [Header("Hull Profile")]
        public float length = 308.0f;
        public float depth = 16.0f;
        public float beam = 38.0f;
        public float designedDraught = 13.0f;

        [NotNull] public AnimationCurve sheerProfile = AnimationCurve.Constant(0.0f, 1.0f, -1.0f);
        [NotNull] public AnimationCurve halfBreadthProfile = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        [NotNull] public AnimationCurve bodyProfile = AnimationCurve.Constant(0.0f, 1.0f, -1.0f);


        [Header("Calulation Steps")]
        [Range(2, 64)] public int lengthSteps = 4;
        [Range(1, 64)] public int beamSteps = 2;
        [Range(1, 64)] public int curveProfilingSteps = 32;
        [Range(0.0f, 1.0f)] public float beamDraughtSamplingCurve = 0.1f;
        private float seaLevel;
        private AnimationCurve[] crossSectionAreaByDraughtProfiles;
        private int blocks;
        private float db;
        private float rho;
        private float mu;
        [NonSerialized] public float[] blockDraughts;
        [NonSerialized] public float[] blockBuoyancies;
        [NonSerialized] public Vector3[] blockBottomPoints;
        private AnimationCurve[] blockVolumeProfiles;
        private AnimationCurve[] blockSurfaceProfiles;
        private Vector3[] blockLocalForceList;
        private bool initialized;
        private Rigidbody vesselRigidbody;
        [NonSerialized] public Ocean ocean;
        private AnimationCurve forwardCTProfile;
        private AnimationCurve sideCTProfile;
        private AnimationCurve verticalCTProfile;
        private UdonSharpBehaviour[] appendages;

        private void Start()
        {
            UpdateProfiles();
            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!vesselRigidbody) return;
            var gravity = Physics.gravity;

            for (var index = 0; index < blocks; index++)
            {
                var force = GetBlockBuoyancy(index, gravity) - transform.TransformVector(blockLocalForceList[index]);
                if (float.IsNaN(force.x))
                {
                    Debug.Log($"[{vesselRigidbody.gameObject.name}][{index}] fr={blockLocalForceList[index]}, fb={GetBlockBuoyancy(index, gravity)}");
                }
                vesselRigidbody.AddForceAtPosition(GetBlockBuoyancy(index, gravity) - transform.TransformVector(blockLocalForceList[index]), GetBlockCenterOfBuoyancy(index));
            }
        }

        private void Update()
        {
            if (!initialized) return;

            var velocity = vesselRigidbody.velocity;
            var angularVelocity = vesselRigidbody.angularVelocity;
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var g = Physics.gravity.magnitude;

            for (var index = 0; index < blocks; index++)
            {
                var bottomPoint = blockBottomPoints[index];
                var p = transform.TransformPoint(bottomPoint);
                var d = Mathf.Clamp(seaLevel - p.y, 0.0f, -bottomPoint.y);
                var v = blockVolumeProfiles[index].Evaluate(d);

                blockDraughts[index] = d;
                blockBuoyancies[index] = rho * v;

                if (d > 0)
                {
                    var s = blockSurfaceProfiles[index].Evaluate(d);
                    var centerOfBuoyancy = p + transform.up * d;
                    var blockLocalVelocity = transform.InverseTransformVector(velocity + Vector3.Cross(angularVelocity, centerOfBuoyancy - centerOfMass));

                    blockLocalForceList[index] =
                        Vector3.forward * EvaluateRegistanceForce(forwardCTProfile, length, s, blockLocalVelocity.z, g)
                        + Vector3.right * EvaluateRegistanceForce(sideCTProfile, beam, s, blockLocalVelocity.x, g)
                        + Vector3.up * EvaluateRegistanceForce(verticalCTProfile, d, s, blockLocalVelocity.y, g);
                    if (float.IsNaN(blockLocalForceList[index].x))
                    {
                        Debug.Log($"[{vesselRigidbody.gameObject.name}][{index}] f={blockLocalForceList[index]}, b={bottomPoint}, p={p}, d={d}, v={v}, s={s}");
                    }
                }
                else
                {
                    blockLocalForceList[index] = Vector3.zero;
                }
            }
        }

        [PublicAPI]
        public void UpdateProfiles()
        {
            vesselRigidbody = GetComponentInParent<Rigidbody>();

            ocean = vesselRigidbody.GetComponentInParent<Ocean>();

            seaLevel = ocean ? ocean.transform.position.y : 0.0f;
            rho = ocean ? ocean.rho : 1025.0f;
            mu = ocean ? ocean.rho : 0.00122f;

            crossSectionAreaByDraughtProfiles = new AnimationCurve[lengthSteps];

            blocks = lengthSteps * beamSteps * 2;
            blockDraughts = new float[blocks];
            blockBuoyancies = new float[blocks];
            blockBottomPoints = new Vector3[blocks];
            blockLocalForceList = new Vector3[blocks];
            blockVolumeProfiles = new AnimationCurve[blocks];
            blockSurfaceProfiles = new AnimationCurve[blocks];

            db = beam / beamSteps * 0.5f;

            for (var i = 0; i < lengthSteps; i++)
            {
                var v = (i + 0.5f) / lengthSteps;

                var bodyProfile = GetCrossSectionBodyProfileAt(v);
                var depthProfile = GetCrossSectionDepthProfileAt(v);

                crossSectionAreaByDraughtProfiles[i] = GetCrossSectionAreaByDraughtProfile(v);

                var cp = Vector3.back * (v - 0.5f) * length;

                for (var j = 0; j < beamSteps; j++)
                {
                    var u = (j + 0.5f) / beamSteps;

                    var index = GetBlockIndex(i, j);
                    var b = bodyProfile.Evaluate(u) * 0.5f;
                    var d = depthProfile.Evaluate(u);

                    blockBottomPoints[index] = cp + Vector3.right * b + Vector3.down * d;
                    blockBottomPoints[index + 1] = cp + Vector3.left * b + Vector3.down * d;
                    blockVolumeProfiles[index] = blockVolumeProfiles[index + 1] = GetBlockVolumeProfile(u, v);
                    blockSurfaceProfiles[index] = blockSurfaceProfiles[index + 1] = GetBlockSurfaceProfile(u, v);
                }
            }

            appendages = GetAppendages();

            var maxSpeed = 100.0f;
            forwardCTProfile = GetSideCTProfile(maxSpeed);
            sideCTProfile = GetForwardCTProfile(maxSpeed);
            verticalCTProfile = GetVerticalCTProfile(maxSpeed);

            foreach (var screwPropeller in vesselRigidbody.GetComponentsInChildren<ScrewPropeller>())
            {
                screwPropeller.etaH = GetScrewPropellerHullEfficiency(maxSpeed, screwPropeller.diameter);
                screwPropeller.etaR = GetRelativeRotativeEfficiency(screwPropeller.pitch, screwPropeller.diameter);
            }
        }

        private UdonSharpBehaviour[] GetAppendages()
        {
            var hullAppendages = vesselRigidbody.GetComponentsInChildren<HullAppendage>();
            var rudders = vesselRigidbody.GetComponentsInChildren<Rudder>();
            var bildgeKeels = vesselRigidbody.GetComponentsInChildren<BilgeKeel>();

            var appendages = new UdonSharpBehaviour[hullAppendages.Length + rudders.Length + bildgeKeels.Length];
            var i = 0;

            Array.Copy(hullAppendages, 0, appendages, i, hullAppendages.Length);
            i += hullAppendages.Length;

            Array.Copy(rudders, 0, appendages, i, rudders.Length);
            i += rudders.Length;

            Array.Copy(bildgeKeels, 0, appendages, i, bildgeKeels.Length);
            // i += bildgeKeels.Length;

            return appendages;
        }

        private float GetK(float cb, float l, float b, float d)
        {
            return 0.0905f + 1.51f * cb / (Mathf.Pow(l/b, 2.0f) * Mathf.Sqrt(b / d));
        }

        public Vector3 GetFormFactor()
        {
            var cb = GetVolume(depth) / (beam * length * depth);
            return Vector3.one + new Vector3(GetK(cb, beam, length, depth), GetK(cb, depth, beam, depth), GetK(cb, length, beam, depth));
        }

        public int GetBlockIndex(int lengthIndex, int beamIndex)
        {
            return (lengthIndex * beamSteps + beamIndex) * 2;
        }

        public float GetKeelDepthAt(float v)
        {
            return -sheerProfile.Evaluate(v) * depth;
        }

        public float GetDepthAt(float u, float v)
        {
            return GetKeelDepthAt(v) * -bodyProfile.Evaluate(u);
        }

        public float GetBeamAt(float v)
        {
            return halfBreadthProfile.Evaluate(v) * beam;
        }

        public Vector3 GetBlockCenterOfBuoyancy(int index)
        {
            return transform.TransformPoint(blockBottomPoints[index] + Vector3.up * blockDraughts[index] * 0.5f);
        }

        public Vector3 GetBlockBuoyancy(int index, Vector3 gravity)
        {
            return -gravity * blockBuoyancies[index];
        }

        public float GetBlockLength()
        {
            return length / lengthSteps;
        }
        public float GetBlockWidth(float v)
        {
            return GetBeamAt(v) * 0.5f / beamSteps;
        }

        public float GetBlockCrossSectionArea(float u, float v)
        {
            var b = GetBeamAt(v) * u;
            var d = GetDepthAt(u, v);
            return b * d;
        }

        public float GetDraughtAt(float u, float v, float draught)
        {
            return DepthToDraught(GetDepthAt(u, v), draught);
        }

        public float DepthToDraught(float d, float draught)
        {
            return Mathf.Clamp(draught - depth + d, 0.0f, d);
        }

        public AnimationCurve GetCrossSectionBodyProfileAt(float v)
        {
            var result = CreateAnimationCurve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = (float)i / curveProfilingSteps;
                result.AddKey(u, GetBeamAt(v) * u);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public AnimationCurve GetCrossSectionDepthProfileAt(float v)
        {
            var result = CreateAnimationCurve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = (float)i / curveProfilingSteps;
                result.AddKey(u, GetDepthAt(u, v));
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public AnimationCurve GetCrossSectionAreaByDraughtProfile(float v)
        {
            var result = CreateAnimationCurve();

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

        public AnimationCurve GetCrossSectionArcByDraughtProfileAt(float v)
        {
            var segments = CreateAnimationCurve();
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

            var result = CreateAnimationCurve();
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

        public AnimationCurve GetCrossSectionBeamByDraughtProfileAt(float v)
        {
            var result = CreateAnimationCurve();

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

        public AnimationCurve GetVolumeByDraughtProfile()
        {
            var result = CreateAnimationCurve();

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

        public AnimationCurve GetSurfaceAreaByDraughtProfile()
        {
            var result = CreateAnimationCurve();

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

        public AnimationCurve GetWaterplaneAreaByDraughtProfile()
        {
            var result = CreateAnimationCurve();

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


        public AnimationCurve GetBeamByDraughtProfile()
        {
            var result = CreateAnimationCurve();

            for (var i = 0; i <= curveProfilingSteps; i++)
            {
                var u = Mathf.Pow((float)i / curveProfilingSteps, beamDraughtSamplingCurve);
                var draught = depth * (1 + bodyProfile.Evaluate(u));
                result.AddKey(draught, u * beam);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public AnimationCurve GetWaterLineLengthByDraughtProfile()
        {
            var bowSide = CreateAnimationCurve();
            var sternSide = CreateAnimationCurve();

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

            var result = CreateAnimationCurve();
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

        public AnimationCurve GetBlockVolumeProfile(float u, float v)
        {
            var result = CreateAnimationCurve();

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

        public AnimationCurve GetBlockSurfaceProfile(float u, float v)
        {
            var result = CreateAnimationCurve();

            var l = GetBlockLength();
            var a = 0.0f;

            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var u1 = u + Mathf.Max((float)i / curveProfilingSteps - 0.5f) / beamSteps;
                var u2 = u + Mathf.Max((float)(i + 1) / curveProfilingSteps - 0.5f) / beamSteps;

                var d1 = GetDepthAt(u1, v);
                var d2 = GetDepthAt(u2, v);

                var w = Vector2.Distance(new Vector2(u1, d1), new Vector2(u2, d2));
                a += w * l;

                result.AddKey(depth - (d1 + d2) / 2.0f, a);
            }

            CurveSmoothTangents(result, 0.0f);

            return result;
        }

        public float GetVolume(float draught)
        {
            var volume = 0.0f;
            var l = length / lengthSteps;

            for (var i = 0; i < lengthSteps; i++)
            {
                var v = (i + 0.5f) / lengthSteps;
                volume += GetCrossSectionAreaByDraughtProfile(v).Evaluate(draught) * l;
            }

            return volume;
        }

        private AnimationCurve CreateAnimationCurve()
        {
            var curve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            while (curve.length > 0) curve.RemoveKey(0);
            return curve;
        }

        public void CurveSmoothTangents(AnimationCurve curve, float weight)
        {
            for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, weight);
        }

        public float EvaluateRegistanceForce(AnimationCurve crProfile, float l, float s, float v, float g)
        {
            var absV = Mathf.Abs(v);
            var fn = GetFn(l, absV, g);
            var cr = crProfile.Evaluate(fn);
            return 0.5f * rho * Mathf.Pow(absV, 2.0f) * s * cr * Mathf.Sign(v);
        }

        #region Fluid Utilities
        public float GetRn(float rho, float mu, float l, float v)
        {
            return v * l * rho / mu;
        }

        public float GetFn(float l, float v, float g)
        {
            return v / Mathf.Sqrt(l * g);
        }

        public float GetResistanceForce(float cr, float rho, float s, float v)
        {
            return 0.5f * rho * Mathf.Pow(v, 2.0f) * s * cr * Mathf.Sign(v);
        }

        public float GetResistanceCoefficient(float r, float rho, float s, float v)
        {
            return r / (0.5f * rho * Mathf.Pow(v, 2.0f) * s);
        }
        #endregion

        #region ITTC1957
        public float GetCF(float rn)
        {
            return 0.075f / Mathf.Pow((Mathf.Log(rn) / Mathf.Log(10)) - 2.0f, 2.0f);
        }
        #endregion

        #region HoltropMennenMethod
        private float GetLR(float cp, float lcb)
        {
            return length * (1.0f - cp + 0.06f * cp * lcb / (4.0f * cp - 1.0f));
        }

        public float GetK1(float cp, float lcb)
        {
            var cs = 0.0f;

            var lr = GetLR(cp, lcb);
            var t_l = depth / length;
            var c12 = t_l > 0.05f ? Mathf.Pow(t_l, 0.22288446f) : t_l > 0.02f ? (48.20f * Mathf.Pow(t_l - 0.02f, 2.078f) + 0.479948f) : 0.479948f;
            var c13 = 1.0f + 0.003f * cs;
            return c13 * (0.93f + c12 * Mathf.Pow(beam / lr, 0.92497f) * Mathf.Pow(0.95f - cp, -0.521448f) * Mathf.Pow(1 - cp + 0.0225f * lcb, 0.6906f)) - 1.0f;
        }

        public float GetCW(float s, float volume, float cp, float cm, float cw, float fn, float g, float v, float at, float hb, float abt, float tf, float lcb)
        {
            var t = depth;
            var l = length;
            var b = beam;

            var lr = GetLR(cp, lcb);
            var l_3 = Mathf.Pow(l, 3.0f);
            var l_b = l / b;
            var l_3_volume = l_3 / volume;
            var b_l = b / l;

            var c3 = 0.56f * Mathf.Pow(abt, 1.5f) / (b * t * (0.31f * Mathf.Sqrt(abt) + tf - hb));

            var c2 = Mathf.Exp(-1.89f * Mathf.Sqrt(c3));
            var c5 = 1.0f - 0.8f * at / (b * t * cm);
            var c7 = b_l < 0.11f ? 0.229577f * Mathf.Pow(b_l, 0.33333f) : b_l < 0.25f ? b_l : 0.5f - 0.0625f * l / b;

            var ie = 1.0f + 89.0f * Mathf.Exp(-Mathf.Pow(l / b, 0.80856f) * Mathf.Pow(1.0f - cw, 0.30484f) * Mathf.Pow(1.0f - cp - 0.0025f * lcb, 0.6367f) * Mathf.Pow(lr / b, 0.34574f) * Mathf.Pow(100 * volume / l_3, 0.16302f));

            var c1 = 2223105.0f * Mathf.Pow(c7, 3.78616f) * Mathf.Pow(t / b, 1.07961f) * Mathf.Pow(90 - ie, -1.37565f);

            var λ = l_b < 12 ? 1.446f * cp - 0.03f * l_b : 1.466f * cp - 0.36f;

            var c15 = l_3_volume < 512.0f ? -1.69385f : l_3_volume > 1727.0f ? 0.0f : -1.69385f + (l / Mathf.Pow(volume, 1.0f / 3.0f) - 8.0f) / 2.36f;

            var d = -0.9f;

            var c16 = cp < 0.80f ? 8.07981f * cp - 13.8673f * Mathf.Pow(cp, 2.0f) + 6.984388f * Mathf.Pow(cp, 3.0f) : 1.73014f - 0.7067f * cp;

            var m1 = 0.0140407f * l / t - 0.175254f * Mathf.Pow(volume, 1.0f / 3.0f) / l - 4.79323f * b / l - c16;
            // var m2 = c15 * Mathf.Pow(cp, 2.0f) * Mathf.Exp(-0.1f * Mathf.Pow(fn, -2.0f));
            var m4 = c15 * 0.5f * Mathf.Exp(-0.034f * Mathf.Pow(fn, -3.29f));
            var m2 = m4;

            return c1 * c2 * c5 * volume * g * Mathf.Exp(m1 * Mathf.Pow(fn, d) + m2 * Mathf.Cos(λ * Mathf.Pow(fn, -2.0f))) / (0.5f * Mathf.Pow(v, 2.0f) * s);
        }

        public float GetCT(
            float l,
            float v,
            float cp,
            float cm,
            float cw,
            float g,
            float volume,
            float surface
        )
        {
            var fn = GetFn(l, v, g);
            var lcb = 0.0f;
            var at = 0.0f;
            var tf = designedDraught;
            var hb = tf / 2.0f;
            var f = GetCF(GetRn(rho, mu, l, volume));
            var w = GetCW(surface, volume, cp, cm, cw, fn, g, v, at, hb, 0.0f, tf, lcb);
            var k1 = GetK1(cp, lcb);
            return f * (1 + k1) + w;
        }

        public AnimationCurve GetCTProfile(float l, float maxSpeed, float cm, float cw)
        {
            var g = Physics.gravity.magnitude;
            var result = CreateAnimationCurve();
            var volume = GetVolume(designedDraught);
            var surface = GetSurfaceAreaByDraughtProfile().Evaluate(designedDraught);
            var am = GetCrossSectionAreaByDraughtProfile(0.5f).Evaluate(designedDraught);
            var cp = volume / (l * am);

            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var speed = maxSpeed * (i + 0.5f) / curveProfilingSteps;
                var fn = GetFn(l, speed, g);
                var ct = GetCT(l, speed, cp, cm, cw, g, volume, surface);

                result.AddKey(fn, ct);
            }
            CurveSmoothTangents(result, 1.0f);

            return result;
        }

        public AnimationCurve GetForwardCTProfile(float maxSpeed)
        {
            var l = GetWaterLineLengthByDraughtProfile().Evaluate(designedDraught);
            var cm = GetCrossSectionAreaByDraughtProfile(0.5f).Evaluate(designedDraught) / (beam * designedDraught);
            var cw = GetWaterplaneAreaByDraughtProfile().Evaluate(designedDraught) / (beam *  l);
            return GetCTProfile(l, maxSpeed, cm, cw);
        }
        public AnimationCurve GetSideCTProfile(float maxSpeed)
        {
            var l = GetWaterLineLengthByDraughtProfile().Evaluate(designedDraught);
            var midshipSectionArea = 0.0f;
            var dv = l / curveProfilingSteps;
            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var v = (i + 0.5f) / curveProfilingSteps;
                var d = Mathf.Max(designedDraught - GetKeelDepthAt(v), 0.0f);
                midshipSectionArea += d * dv;
            }
            var cm = midshipSectionArea / (l * designedDraught);
            var cw = GetWaterplaneAreaByDraughtProfile().Evaluate(designedDraught) / (beam *l);
            return GetCTProfile(length, maxSpeed, cm, cw);
        }
        public AnimationCurve GetVerticalCTProfile(float maxSpeed)
        {
            var cm = GetWaterplaneAreaByDraughtProfile().Evaluate(designedDraught) / (beam * GetWaterLineLengthByDraughtProfile().Evaluate(designedDraught));
            var cw = cm;
            return GetCTProfile(length, maxSpeed, cm, cw);
        }

        public AnimationCurve GetScrewPropellerHullEfficiency(float maxSpeed, float d)
        {
            var result = CreateAnimationCurve();

            var volume = GetVolume(designedDraught);
            var surface = GetSurfaceAreaByDraughtProfile().Evaluate(designedDraught);
            var cb = GetVolume(depth) / (beam * length * designedDraught);
            var am = GetCrossSectionAreaByDraughtProfile(0.5f).Evaluate(designedDraught);
            var cp = volume / (length * am);
            var ca = 0.0f;
            var lcb = 0.0f;
            var k = GetK1(cp, lcb);

            for (var i = 0; i < curveProfilingSteps; i++)
            {
                var v = (i + 0.5f) / curveProfilingSteps * maxSpeed;

                var rn = GetRn(rho, mu, length, v);
                var cf = GetCF(rn);
                var cv = (1 + k) * cf + ca;

                var w = 0.3905f * cb + 0.03905f * cv * cb - 0.1f;
                var t = 0.325f * cb - 0.1885f * d / Mathf.Sqrt(beam * designedDraught);

                var etaH = (1.0f - t) / (1.0f - w);
                result.AddKey(v, etaH);
            }

            CurveSmoothTangents(result, 0.0f);

            return result;
        }

        public float GetRelativeRotativeEfficiency(float p, float d)
        {
            var volume = GetVolume(designedDraught);
            var am = GetCrossSectionAreaByDraughtProfile(0.5f).Evaluate(designedDraught);
            var cp = volume / (length * am);
            var lcb = 0.0f;
            return 0.9737f + 0.111f * (cp - 0.0225f * lcb) - 0.0635f * p / d;
        }
        #endregion

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            this.UpdateProxy();

            if (!initialized) return;

            var gravity = Physics.gravity;
            var mass = vesselRigidbody?.mass ?? GetVolume(0.75f) * rho;
            var centerOfMass = vesselRigidbody.worldCenterOfMass;
            var velocity = vesselRigidbody.velocity;
            var angularVelocity = vesselRigidbody.angularVelocity;

            var forceScale = SceneView.currentDrawingSceneView.size * 9.81f / mass;

            for (var index = 0; index < blocks; index++)
            {
                var cob = GetBlockCenterOfBuoyancy(index);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(cob, db * 0.5f);
                Gizmos.DrawRay(cob, GetBlockBuoyancy(index, gravity) * forceScale);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(cob, -transform.TransformVector(blockLocalForceList[index]) * forceScale);

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(cob, velocity + Vector3.Cross(angularVelocity, cob - centerOfMass));
            }

            Gizmos.color = Color.red;
            Gizmos.DrawRay(centerOfMass, gravity * forceScale * mass);
        }
#endif
    }
}
