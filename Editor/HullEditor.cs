using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace USS
{
    [CustomEditor(typeof(Hull))]
    public class HullEditor : Editor
    {
        public enum SpeedUnit {
            MeterPerSecond,
            Knot,
        }

        private static int gizmoSubdivisions = 4;
        private static float v = 0.5f;
        private int speedUnit = (int)SpeedUnit.Knot;

        private AnimationCurve midshipSectionAreaByDraughtProfile;
        private AnimationCurve waterplaneAreaByDraughtProfile;
        private AnimationCurve beamByDraughtProfile;
        private AnimationCurve waterlineLegthByDraughtProfile;
        private Ocean ocean;
        private AnimationCurve surfaceAreaByDraughtProfile;
        private AnimationCurve volumeByDraughtProfile;
        private AnimationCurve crossSectionAreaByDraughtProfile;
        private AnimationCurve crossSectionArcByDraughtProfile;
        private AnimationCurve crossSectionDepthProfile;
        private AnimationCurve crossSectionBeamByDraughtProfile;
        static private Vector2Int block;
        private AnimationCurve blockSurfaceProfile;
        private AnimationCurve blockVolumeProfile;
        private AnimationCurve rtz;
        private float maxSpeed = 15.43332f * 2.0f;
        private float speed = 15.43332f;
        private AnimationCurve sumOfBlockSurfaceProfile;
        private AnimationCurve sumOfBlockVolumeProfile;
        private AnimationCurve rfz;
        private float k1z;
        private AnimationCurve rwz;
        private AnimationCurve rfmz;
        private HullDimension h;
        private AnimationCurve rtfnz;
        private AnimationCurve ctfnz;
        private AnimationCurve forwardCTProfile;
        private AnimationCurve sideCTProfile;
        private AnimationCurve verticalCTProfile;
        private AnimationCurve forwardRTProfile;
        private AnimationCurve sideRTProfile;
        private AnimationCurve verticalRTProfile;

        private static float GetDraught(Hull hull, Ocean ocean)
        {
            return Mathf.Clamp((ocean? ocean.transform.position.y : 0.0f) - (hull.transform.position.y - hull.transform.up.y * hull.depth), 0, hull.depth);
        }

        private void OnEnable()
        {
            var hull = target as Hull;
            if (!hull) return;

            UpdateProfiles(hull);

            block = Vector2Int.up * hull.lengthSteps / 2;
        }

        private void UpdateProfiles(Hull hull)
        {
            hull.UpdateProfiles();
            ocean = hull.GetComponentInParent<Rigidbody>().GetComponentInParent<Ocean>() ?? new Ocean();
            surfaceAreaByDraughtProfile = hull.GetSurfaceAreaByDraughtProfile();
            volumeByDraughtProfile = hull.GetVolumeByDraughtProfile();
            midshipSectionAreaByDraughtProfile = hull.GetCrossSectionAreaByDraughtProfile(0.5f);
            waterplaneAreaByDraughtProfile = hull.GetWaterplaneAreaByDraughtProfile();
            beamByDraughtProfile = hull.GetBeamByDraughtProfile();
            waterlineLegthByDraughtProfile = hull.GetWaterLineLengthByDraughtProfile();

            var curveSteps = Enumerable.Range(0, hull.curveProfilingSteps + 1)
                    .Select(i => (float)i / hull.curveProfilingSteps);
            var blocks = Enumerable.Range(0, hull.lengthSteps).Select(i => (i + 0.5f) / hull.lengthSteps).Zip(Enumerable.Range(0, hull.beamSteps).Select(i => (i + 0.5f) / hull.beamSteps), (v, u) => (u, v)).ToArray();

            var blockSurfaceProfiles = blocks.Select(block => hull.GetBlockSurfaceProfile(block.u, block.v)).ToArray();
            var blockVolumeProfiles = blocks.Select(block => hull.GetBlockVolumeProfile(block.u, block.v)).ToArray();
            var draughts = curveSteps.Select(f => f * hull.depth).ToArray();

            sumOfBlockSurfaceProfile = new AnimationCurve(draughts.Select(d => new Keyframe(d, blockSurfaceProfiles.Sum(p => p.Evaluate(d)) * 2.0f)).ToArray());
            sumOfBlockVolumeProfile = new AnimationCurve(draughts.Select(d => new Keyframe(d, blockVolumeProfiles.Sum(p => p.Evaluate(d)) * 2.0f)).ToArray());

            UpdateCrossSectionProfiles(hull);
            UpdateResistanceProfiles(hull);
            UpdateBlockProfile(hull);
        }

        private void UpdateCrossSectionProfiles(Hull hull)
        {
            crossSectionAreaByDraughtProfile = hull.GetCrossSectionAreaByDraughtProfile(v);
            crossSectionArcByDraughtProfile = hull.GetCrossSectionArcByDraughtProfileAt(v);
            crossSectionDepthProfile = hull.GetCrossSectionDepthProfileAt(v);
            crossSectionBeamByDraughtProfile = hull.GetCrossSectionBeamByDraughtProfileAt(v);
        }

        private void UpdateResistanceProfiles(Hull hull)
        {
            var speeds = Enumerable.Range(0, hull.curveProfilingSteps).Select(i => (float)i / hull.curveProfilingSteps * maxSpeed);

            var fluid = Fluid.OceanWater;

            var draught = GetDraught(hull, ocean);
            h = new HullDimension()
            {
                t = draught,
                b = beamByDraughtProfile.Evaluate(draught),
                l = waterlineLegthByDraughtProfile.Evaluate(draught),
                v = volumeByDraughtProfile.Evaluate(draught),
                am = midshipSectionAreaByDraughtProfile.Evaluate(draught),
                aw = waterplaneAreaByDraughtProfile.Evaluate(draught),

                afterbodyForm = AfterbodyForm.NormalSectionShape,
                hasBulbousBow = false,
                hasTransom = false,
            };

            var g = Physics.gravity.magnitude;
            var ρ = ocean.rho;
            var μ = ocean.mu;

            var transom = false;

            var tf = draught; // ToDo
            var ta = draught;

            var lcb = 0.0f; // ToDo
            var at = transom ? 0.95f * (ta - ta * 0.9225f) * h.b * 0.89f : 0.0f;
            var abt = HoltropMennen.GetABT(h, tf);
            var hb = tf / 2.0f;

            // var s = surfaceAreaByDraughtProfile.Evaluate(t);
            h.s = HoltropMennen.GetS(h, tf);

            k1z = HoltropMennen.GetK1(h, lcb);
            var rxz =
                speeds.Select(v =>
                {
                    var re = fluid.GetRn(h.l, v);
                    var rf = HoltropMennen.GetRF(fluid, h, v);

                    var rapp = 0.0f; // ToDo

                    var fn = h.GetFn(v, g);
                    var rw = HoltropMennen.GetRW(fluid, h, g, v, at, hb, abt, tf,lcb);

                    var rb = 0.0f; // ToDo
                    var rtr = 0.0f; // ToDo
                    var ra = 0.0f; // ToDo

                    var rt = HoltropMennen.GetRt(rf, k1z, rapp, rw, rb, rtr, ra);

                    return (v, re, fn, rf, rapp, rw, rb, rtr, ra, rt);
                })
                .ToArray();

            rfz = new AnimationCurve(rxz.Select(a => new Keyframe(a.v, a.rf)).ToArray());
            hull.CurveSmoothTangents(rfz, 1.0f);
            rfmz = new AnimationCurve(rxz.Select(a => new Keyframe(a.v, a.rf * k1z)).ToArray());
            hull.CurveSmoothTangents(rfmz, 1.0f);
            rwz = new AnimationCurve(rxz.Select(a => new Keyframe(a.v, a.rw)).ToArray());
            hull.CurveSmoothTangents(rwz, 1.0f);
            rtz = new AnimationCurve(rxz.Select(a => new Keyframe(a.v, a.rt)).ToArray());
            hull.CurveSmoothTangents(rtz, 1.0f);
            rtfnz = new AnimationCurve(rxz.Select(a => new Keyframe(a.fn, a.rt)).ToArray());
            hull.CurveSmoothTangents(rtfnz, 1.0f);
            ctfnz = new AnimationCurve(rxz.Select(a => new Keyframe(a.fn, fluid.GetResistanceCoefficient(a.rt, h.s, a.v))).ToArray());
            hull.CurveSmoothTangents(ctfnz, 1.0f);

            forwardCTProfile = hull.GetForwardCTProfile(maxSpeed);
            sideCTProfile = hull.GetSideCTProfile(maxSpeed);
            verticalCTProfile = hull.GetVerticalCTProfile(maxSpeed);

            forwardRTProfile = new AnimationCurve(speeds.Select(v => new Keyframe(v, hull.GetResistanceForce(forwardCTProfile.Evaluate(hull.GetFn(hull.length, v, g)), ρ, h.s, v))).ToArray());
            hull.CurveSmoothTangents(forwardRTProfile, 1.0f);
            sideRTProfile = new AnimationCurve(speeds.Select(v => new Keyframe(v, hull.GetResistanceForce(sideCTProfile.Evaluate(hull.GetFn(hull.beam, v, g)), ρ, h.s, v))).ToArray());
            hull.CurveSmoothTangents(sideRTProfile, 1.0f);
            verticalRTProfile = new AnimationCurve(speeds.Select(v => new Keyframe(v, hull.GetResistanceForce(verticalCTProfile.Evaluate(hull.GetFn(draught, v, g)), ρ, h.s, v))).ToArray());
            hull.CurveSmoothTangents(verticalRTProfile, 1.0f);
        }

        private void UpdateBlockProfile(Hull hull)
        {
            var u = (float)block.x / hull.beamSteps;
            var v = (float)block.y / hull.lengthSteps;

            blockSurfaceProfile = hull.GetBlockSurfaceProfile(u, v);
            blockVolumeProfile = hull.GetBlockVolumeProfile(u, v);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var hull = target as Hull;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                serializedObject.Update();

                var property = serializedObject.GetIterator();
                property.NextVisible(true);

                while (property.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                serializedObject.ApplyModifiedProperties();

                if (change.changed) UpdateProfiles(hull);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Specs", EditorStyles.boldLabel);

                var draught = GetDraught(hull, ocean);
                var waterlineLength = waterlineLegthByDraughtProfile.Evaluate(draught);
                var breadth = beamByDraughtProfile.Evaluate(draught);
                var volume = volumeByDraughtProfile.Evaluate(draught);

                var midshipSectionArea = midshipSectionAreaByDraughtProfile.Evaluate(draught);
                var waterplaneArea = waterplaneAreaByDraughtProfile.Evaluate(draught);

                var cm = midshipSectionArea / (breadth * draught);
                var cw = waterplaneArea / (hull.beam * hull.length);
                var cb = volume / (waterlineLength * breadth * draught);
                var cp = volume / (waterlineLength * midshipSectionArea);
                var cvp = volume / (waterplaneArea * draught);

                var abt = HoltropMennen.GetABT(h, draught);

                EditorGUILayout.LabelField("Lpp", $"{hull.length:F2}m");
                EditorGUILayout.LabelField("Lwl", $"{waterlineLength:F2}m");
                EditorGUILayout.LabelField("B", $"{breadth:F2}m");
                EditorGUILayout.LabelField("d", $"{draught:F2}m");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Δ", $"{volume * ocean.rho / 1000.0f:F2}t");
                EditorGUILayout.LabelField("▽", $"{volume:F2}㎥");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("S", $"{surfaceAreaByDraughtProfile.Evaluate(draught):F2}㎡");
                EditorGUILayout.LabelField("S (estimated)", $"{HoltropMennen.GetS(h, abt):F2}㎡");
                EditorGUILayout.LabelField("AM", $"{h.am}㎡");
                EditorGUILayout.LabelField("AW", $"{h.aw}㎡");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("CM", $"{h.CM:F2}");
                EditorGUILayout.LabelField("CW", $"{h.CW:F2}");
                EditorGUILayout.LabelField("CB", $"{h.CB:F2}");
                EditorGUILayout.LabelField("CP", $"{h.CP:F2}");
                EditorGUILayout.LabelField("CVP", $"{h.CVP:F2}");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("by Draught Profiles", EditorStyles.boldLabel);
                EditorGUILayout.CurveField("Lwl", waterlineLegthByDraughtProfile);
                EditorGUILayout.CurveField("Breadth", beamByDraughtProfile);
                EditorGUILayout.CurveField("Waterplane Area", waterplaneAreaByDraughtProfile);
                EditorGUILayout.CurveField("Midship Section Area", midshipSectionAreaByDraughtProfile);
                EditorGUILayout.CurveField("Volume", volumeByDraughtProfile);

                EditorGUILayout.Space();

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    v = EditorGUILayout.Slider("Cross Section", v, 0.0f, 1.0f);
                    if (change.changed) UpdateCrossSectionProfiles(hull);
                }

                var crossSectionBeam = crossSectionBeamByDraughtProfile.Evaluate(draught);
                EditorGUILayout.LabelField("Breadth", $"{crossSectionBeam:F2}m");
                EditorGUILayout.LabelField("Arc Length", $"{crossSectionArcByDraughtProfile.Evaluate(draught):F2}m");

                var crossSectionArea = crossSectionAreaByDraughtProfile.Evaluate(draught);
                EditorGUILayout.LabelField("Area", $"{crossSectionArea:F2}㎡");
                EditorGUILayout.LabelField("CB'", $"{crossSectionArea / (crossSectionBeam * draught):F2}");

                EditorGUILayout.CurveField("Depth by U", crossSectionDepthProfile);

                EditorGUILayout.LabelField("by Draught Profiles", EditorStyles.boldLabel);
                EditorGUILayout.CurveField("Breadth", crossSectionBeamByDraughtProfile);
                EditorGUILayout.CurveField("Arc Length", crossSectionArcByDraughtProfile);
                EditorGUILayout.CurveField("Area", crossSectionAreaByDraughtProfile);

                EditorGUILayout.Space();

                var formFactor = hull.GetFormFactor();
                EditorGUILayout.LabelField("Form Factor (1 + k)", $"{formFactor.x:F2}, {formFactor.y:F2}, {formFactor.z:F2}");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Registance Profile", EditorStyles.boldLabel);
                var speedMultiplier = GetSpeedMultiplier((SpeedUnit)speedUnit);
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    maxSpeed = EditorGUILayout.Slider("Max Speed", maxSpeed / speedMultiplier, 0.0f, 100.0f / speedMultiplier) * speedMultiplier;
                    if (change.changed) UpdateResistanceProfiles(hull);
                }
                speed = EditorGUILayout.Slider("Speed", speed / speedMultiplier, 0.0f, maxSpeed / speedMultiplier) * speedMultiplier;
                speedUnit = EditorGUILayout.Popup("Unit", speedUnit, new [] { "MKS", "Knot"});

                EditorGUILayout.LabelField("Total", $"{rtz.Evaluate(speed) / 1000.0f:F2}kN");
                EditorGUILayout.LabelField("Frictional", $"{rfz.Evaluate(speed) / 1000.0f:F2}kN");
                EditorGUILayout.LabelField("Form", $"{rfz.Evaluate(speed) * k1z / 1000.0f:F2}kN");
                EditorGUILayout.LabelField("Wave", $"{rwz.Evaluate(speed) / 1000.0f:F2}kN");
                EditorGUILayout.LabelField("Foam factor (1 + k1)", $"{1 + k1z:F2}");
                EditorGUILayout.CurveField("Total Profile", rtz);
                EditorGUILayout.CurveField("Friction Profile", rfz);
                EditorGUILayout.CurveField("Wave Profile", rwz);
                EditorGUILayout.CurveField("RT vs Fn", rtfnz);
                EditorGUILayout.CurveField("CT vs Fn", ctfnz);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Pre Calculated Ct Profiles", EditorStyles.boldLabel);
                EditorGUILayout.CurveField("Forward", forwardCTProfile);
                EditorGUILayout.CurveField("Side", sideCTProfile);
                EditorGUILayout.CurveField("Vertical", verticalCTProfile);

                EditorGUILayout.LabelField("Pre Calculated Rt Profiles", EditorStyles.boldLabel);
                EditorGUILayout.CurveField("Forward", forwardRTProfile);
                EditorGUILayout.CurveField("Side", sideRTProfile);
                EditorGUILayout.CurveField("Vertical", verticalRTProfile);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Simulation Block Specs", EditorStyles.boldLabel);
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    block.x = EditorGUILayout.IntSlider("Lat", block.x, 0, hull.beamSteps - 1);
                    block.y = EditorGUILayout.IntSlider("Lon", block.y, 0, hull.lengthSteps - 1);
                    if (change.changed) UpdateBlockProfile(hull);
                }

                var blockVolume = blockVolumeProfile.Evaluate(draught);
                var blockSurface = blockSurfaceProfile.Evaluate(draught);

                EditorGUILayout.LabelField("Surface", $"{blockSurface:F2}㎡");
                EditorGUILayout.LabelField("Volume", $"{blockVolume:F2}㎥");
                EditorGUILayout.CurveField("Surface by Draught", blockSurfaceProfile);
                EditorGUILayout.CurveField("Volume by Draught", blockVolumeProfile);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("ΣSurface", $"{sumOfBlockSurfaceProfile.Evaluate(draught):F2}㎡");
                EditorGUILayout.LabelField("ΣVolume", $"{sumOfBlockVolumeProfile.Evaluate(draught):F2}㎡");

                gizmoSubdivisions = EditorGUILayout.IntSlider("Gizmo Subdivisions", gizmoSubdivisions, 1, 64);
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Update Profiles")) hull.UpdateProfiles();
        }

        private float GetSpeedMultiplier(SpeedUnit speedUnit)
        {
            switch (speedUnit)
            {
                case SpeedUnit.MeterPerSecond:
                    return 1.0f;
                case SpeedUnit.Knot:
                    return 0.514444f;
            }
            return float.NaN;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(UdonBehaviour))]
        static void OnDrawEditorGizmos(UdonBehaviour udon, GizmoType gizmoType)
        {
            if (UdonSharpEditorUtility.GetUdonSharpBehaviourType(udon) != typeof(Hull)) return;
            var hull = UdonSharpEditorUtility.GetProxyBehaviour(udon) as Hull;
            var ocean = hull.GetComponentInParent<Rigidbody>().GetUdonSharpComponentInParent<Ocean>();

            try
            {
                Handles.matrix = Gizmos.matrix = hull.transform.localToWorldMatrix;

                DrawHullGizmos(hull);
                DrawDraughtGizmos(hull, ocean);
                DrawCrossSectionGizmos(hull, ocean);
                DrawBlockGizmos(hull);
            }
            finally
            {
                Handles.matrix = Gizmos.matrix = Matrix4x4.identity;
            }
        }

        private static void DrawHullGizmos(Hull hull)
        {
            var length = hull.length;

            var gizmoLengthSteps = hull.lengthSteps * gizmoSubdivisions;
            var gizmoBeamSteps = hull.beamSteps * gizmoSubdivisions;
            var uRange = Enumerable.Range(0, gizmoBeamSteps).Select(n => (float)(n + 0.5f) / gizmoBeamSteps).ToArray();
            var vRange = Enumerable.Range(0, gizmoLengthSteps).Select(n => (float)(n + 0.5f) / gizmoLengthSteps).ToArray();
            var centerPoints = vRange.Select(z => Vector3.back * length * (z - 0.5f));
            var fpp = Vector3.back * length * 0.5f;
            var app = Vector3.forward * length * 0.5f;

            Gizmos.color = Color.white;
            DrawLineStrip(centerPoints.Prepend(fpp).Append(app));

            var beams = vRange.Select(hull.GetBeamAt).ToArray();
            Gizmos.color = Color.green;
            DrawLineStrip(centerPoints.Zip(beams, (cp, b) => cp + Vector3.right * b * 0.5f).Append(fpp).Prepend(app));
            DrawLineStrip(centerPoints.Zip(beams, (cp, b) => cp + Vector3.left * b * 0.5f).Append(fpp).Prepend(app));

            var depths = vRange.Select(hull.GetKeelDepthAt);
            Gizmos.color = Color.blue;
            DrawLineStrip(centerPoints.Zip(depths, (cp, d) => cp + Vector3.down * d).Append(fpp).Prepend(app));

            Gizmos.color = Color.red;
            foreach (var (v, cp, bodyProfile, depthProfile) in vRange.Zip(centerPoints, (v, cp) => (v, cp, bodyProfile: hull.GetCrossSectionBodyProfileAt(v), depthProfile: hull.GetCrossSectionDepthProfileAt(v))))
            {
                var d = hull.GetKeelDepthAt(v);
                var b = hull.GetBeamAt(v);
                var halfPoints = uRange.Select(u => (b: bodyProfile.Evaluate(u), d: depthProfile.Evaluate(u))).Select(t => cp + Vector3.right * t.b * 0.5f + Vector3.down * t.d).Prepend(cp + Vector3.down * d).Append(cp + Vector3.right * b * 0.5f).ToArray();

                DrawLineStrip(halfPoints);
                DrawLineStrip(halfPoints.Select(p => Vector3.Scale(p - cp, Vector3.one - Vector3.right * 2.0f) + cp));
            }

        }

        private static void DrawCrossSectionGizmos(Hull hull, Ocean ocean)
        {
            var gizmoLengthSteps = hull.lengthSteps * gizmoSubdivisions;
            var gizmoBeamSteps = hull.beamSteps * gizmoSubdivisions;

            var draught = GetDraught(hull, ocean);
            var csp = Vector3.back * hull.length * (v - 0.5f);

            Gizmos.color = Color.yellow;
            DrawWireCubeWithDiagonals(csp + Vector3.down * (hull.depth - draught * 0.5f), Vector3.right * hull.beam + Vector3.up * draught);

            Gizmos.color = Color.red;
            var depthProfile = hull.GetCrossSectionDepthProfileAt(v);
            var beamProfile = hull.GetCrossSectionBodyProfileAt(v);
            var hullCrossSectionPoints = Enumerable.Range(0, gizmoBeamSteps)
                .Select(n => (float)n / gizmoBeamSteps)
                .Append(1.0f)
                .Select(u => csp + Vector3.right * beamProfile.Evaluate(u) * 0.5f + Vector3.down * depthProfile.Evaluate(u))
                .ToArray();
            DrawLineStrip(hullCrossSectionPoints);
            DrawLineStrip(hullCrossSectionPoints.Select(p => Vector3.Scale(p, Vector3.one - Vector3.right * 2.0f)));

            var crossSectionArea = hull.GetCrossSectionAreaByDraughtProfile(v).Evaluate(draught);
            Handles.color = Color.white;
            Handles.Label(csp + Vector3.down * (hull.depth - draught * 0.5f), $"{crossSectionArea:F2}㎡");
        }

        private static void DrawDraughtGizmos(Hull hull, Ocean ocean)
        {
            if (EditorApplication.isPlaying) return;

            Gizmos.color = Color.blue;

            for (var i = 0; i < hull.lengthSteps; i++)
            {
                var v = (i + 0.5f) / hull.lengthSteps;

                var bodyProfile = hull.GetCrossSectionBodyProfileAt(v);
                var depthProfile = hull.GetCrossSectionDepthProfileAt(v);

                var cp = Vector3.back * (v - 0.5f) * hull.length;
                var blockWidth = hull.halfBreadthProfile.Evaluate(v) * hull.beam / hull.beamSteps * 0.5f;

                for (var j = 0; j < hull.beamSteps; j++)
                {
                    var u = (j + 0.5f) / hull.beamSteps;

                    var b = bodyProfile.Evaluate(u) * 0.5f;
                    var d = depthProfile.Evaluate(u);

                    var b1 = cp + Vector3.right * b + Vector3.down * d;
                    var b2 = cp + Vector3.left * b + Vector3.down * d;

                    var p1 = hull.transform.TransformPoint(b1);
                    var p2 = hull.transform.TransformPoint(b2);
                    var d1 = Mathf.Clamp((ocean?.transform.position.y ?? 0.0f) - p1.y, 0.0f, -b1.y);
                    var d2 = Mathf.Clamp((ocean?.transform.position.y ?? 0.0f) - p2.y, 0.0f, -b2.y);
                    // var v = d * dl * blockSizes[index];

                    DrawWireCubeWithDiagonals(b1 + Vector3.up * d1 * 0.5f, Vector3.right * blockWidth + Vector3.up * d1 + Vector3.forward * hull.length / hull.lengthSteps);
                    DrawWireCubeWithDiagonals(b2 + Vector3.up * d2 * 0.5f, Vector3.right * blockWidth + Vector3.up * d2 + Vector3.forward * hull.length / hull.lengthSteps);
                }
            }
        }

        private static void DrawBlockGizmos(Hull hull)
        {
            var u = (block.x + 0.5f) / hull.beamSteps;
            var v = (block.y + 0.5f) / hull.lengthSteps;

            var b = hull.GetBeamAt(v);
            var blockSize = new Vector3(
                b / hull.beamSteps * 0.5f,
                hull.depth,
                hull.length / hull.lengthSteps
            );
            var center = new Vector3(
                u * b * 0.5f,
                -hull.depth * 0.5f,
                (0.5f - v) * hull.length
            );

            Gizmos.color = Color.red;

            DrawWireCubeWithDiagonals(center, blockSize);
        }

        private static void DrawWireCubeWithDiagonals(Vector3 center, Vector3 size)
        {
            Gizmos.DrawWireCube(center, size);
            var extents = size / 2.0f;
            Gizmos.DrawLine(center - extents, center + extents);

            var xMirror = Vector3.Scale(extents, Vector3.one - Vector3.right * 2.0f);
            Gizmos.DrawLine(center - xMirror, center + xMirror);

            var yMirror = Vector3.Scale(extents, Vector3.one - Vector3.up * 2.0f);
            Gizmos.DrawLine(center - yMirror, center + yMirror);

            var zMirror = Vector3.Scale(extents, Vector3.one - Vector3.forward * 2.0f);
            Gizmos.DrawLine(center - zMirror, center + zMirror);
        }

        private static void DrawLineStrip(IEnumerable<Vector3> points)
        {
            foreach (var (p1, p2) in points.Zip(points.Skip(1), (p1, p2) => (p1, p2)))
            {
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
