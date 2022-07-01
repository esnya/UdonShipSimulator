using UnityEngine;

namespace USS2
{
    /// <summary>
    /// Holtrop-Mennen method to estimate resistance force of vessel's hull.
    /// </summary>
    public class HoltropMennen
    {
        /// <summary>
        /// Get total resistance.
        /// </summary>
        /// <param name="rf">Frictional resistance according to the ITTC-1957 friction formula</param>
        /// <param name="k1">1 + k1 form factor</param>
        /// <param name="rapp">Resistance of appendages</param>
        /// <param name="rw">Wave-making and wave-breaking resistance</param>
        /// <param name="rb">Additional pressure resistance of bulbous bow</param>
        /// <param name="rtr">Additional pressure resistance of immersed transom stern</param>
        /// <param name="ra">Model-ship crrelation resistance</param>
        /// <returns>Total resistance</returns>
        public static float GetRt(float rf, float k1, float rapp, float rw, float rb, float rtr, float ra)
        {
            return rf * (1 + k1) + rapp + rw + rb + rtr + ra;
        }

        /// <summary>
        /// Get length of run.
        /// </summary>
        /// <param name="l">Waterline length.</param>
        /// <param name="cp">Prismatic coefficient.</param>
        /// <param name="lcb">Longitudinal position of centre of buoyancy. -1 to 1</param>
        /// <returns></returns>
        public static float GetLR(HullDimension h, float lcb)
        {
            var cp = h.CP;
            return h.l * (1.0f - cp + 0.06f * cp * lcb / (4.0f * cp - 1.0f));
        }

        public static float GetRF(Fluid fluid, HullDimension h, float v)
        {
            var rn = fluid.GetRn(h.l, v);
            var cf = ITTC1957.GetCF(rn);
            return fluid.GetRegistanceForce(cf, h.s, v);
        }

        public const float C_STERN_V = -10.0f;
        public const float C_STERN_Normal = 0.0f;
        public const float C_STERN_U = 10.0f;

        public static float GetCS(AfterbodyForm form)
        {
            switch (form)
            {
                case AfterbodyForm.VShapedSections:
                    return -10.0f;
                case AfterbodyForm.UShapdedSectionsWithHongerStern:
                    return 10.0f;
                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Get resistance form factor parameter K1.
        /// </summary>
        /// <param name="h">Hull dimension.</param>
        /// <param name="lcb">Longtional position center of buoyancy on waterline. -1 to 1</param>
        /// <returns>k1</returns>
        public static float GetK1(HullDimension h, float lcb)
        {
            var cp = h.CP;
            var cs = GetCS(h.afterbodyForm);

            var lr = GetLR(h, lcb);
            var t_l = h.t / h.l;
            var c12 = t_l > 0.05f ? Mathf.Pow(t_l, 0.22288446f) : t_l > 0.02f ? (48.20f * Mathf.Pow(t_l - 0.02f, 2.078f) + 0.479948f) : 0.479948f;
            var c13 = 1.0f + 0.003f * cs;
            return c13 * (0.93f + c12 * Mathf.Pow(h.b / lr, 0.92497f) * Mathf.Pow(0.95f - cp, -0.521448f) * Mathf.Pow(1 - cp + 0.0225f * lcb, 0.6906f)) - 1.0f;
        }

        /// <summary>
        /// Wetted area of hull
        /// </summary>
        /// <param name="h">Hull dimension.</param>
        /// <param name="abt">Transverse  sectional area of bulb.</param>
        /// <returns></returns>
        public static float GetS(HullDimension h, float abt)
        {
            var cm = h.CM;
            var cb = h.CB;
            var cw = h.CW;
            return h.l * (2.0f * h.t + h.b) * Mathf.Sqrt(cm) * (0.453f + 0.4425f * cb + -0.2862f * cm - 0.003467f * h.b / h.t + 0.3696f * cw) + 2.38f * abt / cb;
        }

        /// <summary>
        /// Get appendage resistance.
        /// </summary>
        /// <param name="ρ">Density of water.</param>
        /// <param name="v">Hull speed.</param>
        /// <param name="cf">Coefficient of friction.</param>
        /// <param name="apps">Apendage descriptors, Vector2(surface area, 1 + k1)</param>
        /// <returns>Appendage resistance.</returns>
        public static float GetRapp(float ρ, float v, float cf, Vector2[] apps)
        {
            var sapp = 0.0f;
            var ffeq = 0.0f;
            foreach (var app in apps)
            {
                sapp += app.x;
                ffeq += app.y * app.x;
            }
            ffeq /= sapp;
            return 0.5f * ρ * Mathf.Pow(v, 2.0f) * sapp * ffeq * cf;
        }

        /// <summary>
        /// Get resistance of bow thruster tunnnel.
        /// </summary>
        /// <param name="ρ">Water density.</param>
        /// <param name="v">Hull speed.</param>
        /// <param name="d">Tunnel diameter.</param>
        /// <param name="cbto">Coefficient from 0.003 to 0.012.</param>
        /// <returns>Resistance of bow thruster tunnel.</returns>
        public static float GetBowThrusterResistance(float ρ, float v, float d, float cbto)
        {
            return ρ * Mathf.Pow(v, 2.0f) * Mathf.PI * Mathf.Pow(d, 2.0f) * cbto;
        }

        public static float GetFr(float v, float l, float g)
        {
            return v / Mathf.Sqrt(l * g);
        }

        public static float GetC3(HullDimension hull, float abt, float tf, float hb)
        {
            return 0.56f * Mathf.Pow(abt, 1.5f) / (hull.b * hull.t * (0.31f * Mathf.Sqrt(abt) + tf - hb));
        }

        /// <summary>
        /// Get wave resistance.
        /// </summary>
        /// <param name="volume">Volume udner water.</param>
        /// <param name="ρ">Water density.</param>
        /// <param name="g">Gravity strength.</param>
        /// <param name="fn">Froude number</param>
        /// <param name="v">Hull speed.</param>
        /// <param name="b">Breadth.</param>
        /// <param name="l">Length of water line.</param>
        /// <param name="t">Draught.</param>
        /// <param name="at">Transverse area of transom at zero speed.</param>
        /// <param name="hb">Postion of center of transverse area above keel line.</param>
        /// <param name="abt">Transverse area above keel line.</param>
        /// <param name="tf">Forward draught.</param>
        /// <param name="cp">Prismatic coefficient.</param>
        /// <param name="cm">Midship coefficient.</param>
        /// <param name="cwp">Vertical prismatic coefficient.</param>
        /// <param name="lcb">Longitudinal position of center of buoyancy. Forward of 0.5L as a percentage of L.</param>
        /// <returns>Wave registance force.</returns>
        public static float GetRW(Fluid fluid, HullDimension hull, float g, float v, float at, float hb, float abt, float tf, float lcb)
        {
            var t = hull.t;
            var l = hull.l;
            var b = hull.b;
            var volume = hull.v;
            var cp = hull.CP;
            var cm = hull.CM;
            var cw = hull.CW;

            var fn = hull.GetFn(v, g);
            var lr = GetLR(hull, lcb);
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

            return c1 * c2 * c5 * hull.v * fluid.ρ * g * Mathf.Exp(m1 * Mathf.Pow(fn, d) + m2 * Mathf.Cos(λ * Mathf.Pow(fn, -2.0f)));
        }

        /// <summary>
        /// Get resisitance due to presense of bubbous bow.
        /// </summary>
        /// <param name="v">Hull speed.</param>
        /// <param name="ρ">Water density.</param>
        /// <param name="g">Gravity strength.</param>
        /// <param name="tf">Forward draught.</param>
        /// <param name="abt">Transverse area above keel line.</param>
        /// <param name="hb">Postion of center of transverse area above keel line.</param>
        /// <returns></returns>
        public static float GetRB(float v, float ρ, float g, float tf, float abt, float hb)
        {
            var pb = 0.56f * Mathf.Sqrt(abt) / (tf - 1.5f * hb);
            var fni = v / Mathf.Sqrt(g * (tf - hb - 0.25f * Mathf.Sqrt(abt)) + 0.15f * Mathf.Pow(v, 2.0f));
            return 0.11f * Mathf.Exp(-3.0f * Mathf.Pow(pb, -2.0f)) * Mathf.Pow(fni, 3.0f) * Mathf.Pow(abt, 1.5f) * ρ * g / (1 + Mathf.Pow(fni, 2.0f));
        }

        public static float GetRTR()
        {
            return 0.0f;
        }

        public static float GetABT(HullDimension h, float tf)
        {
            return h.hasBulbousBow ? Mathf.PI * Mathf.Pow(tf / 2.0f, 2.0f) / 7.7f : 0.0f;
        }
    }
}