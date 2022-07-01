using System;
using UdonSharp;
using UdonToolkit;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HullAppendage : UdonSharpBehaviour
    {
        public const int RUDDER_BEHIND_SKEG = 1;
        public const int RUDDER_BEHIND_STERN = 2;
        public const int TWIN_SCREW_BALANCE_RUDDERS = 3;
        public const int SHAFT_BRACKETS = 4;
        public const int SKEG = 5;
        public const int STRUT_BOSSINGS = 6;
        public const int HULL_BOSSINGS = 7;
        public const int SHAFTS = 8;
        public const int STABILIZER_FINS = 9;
        public const int DOME = 10;
        public const int BLIGE_KEEL = 11;

        /// <summary>
        /// Type of Appendage.
        /// </summary>
        [Popup("GetAppendageTypes")] public int appendageType = SHAFT_BRACKETS;

        /// <summary>
        /// Size. (breadth, depth, length) or (diameter, N/A, length)
        /// </summary>
        public Vector3 size = new Vector3(0, 6, 4);

        /// <summary>
        /// Shape factor.
        /// </summary>
        [Range(0.0f, 1.0f)] public float shapeFactor = 0.0f;

        [NonSerialized] public float appendageResistanceFactor;
        [NonSerialized] public float surfaceArea;

        private void Start()
        {
            appendageResistanceFactor = Mathf.Lerp(GetMinAppendageResisstanceFactor(), GetMaxAppendageResisstanceFactor(), shapeFactor);
            surfaceArea = GetSurfaceArea();
            enabled = false;
        }

        public float GetMinAppendageResisstanceFactor()
        {
            switch (appendageType)
            {
                case RUDDER_BEHIND_SKEG:
                    return 1.5f;
                case RUDDER_BEHIND_STERN:
                    return 1.3f;
                case TWIN_SCREW_BALANCE_RUDDERS:
                    return 2.8f;
                case SHAFT_BRACKETS:
                    return 3.0f;
                case SKEG:
                    return 1.5f;
                case STRUT_BOSSINGS:
                    return 3.0f;
                case HULL_BOSSINGS:
                    return 2.0f;
                case SHAFTS:
                    return 2.0f;
                case STABILIZER_FINS:
                    return 2.8f;
                case DOME:
                    return 2.7f;
                case BLIGE_KEEL:
                    return 1.4f;
                default:
                    return 1.0f;
            }
        }

        public float GetMaxAppendageResisstanceFactor()
        {
            switch (appendageType)
            {
                case RUDDER_BEHIND_SKEG:
                    return 2.0f;
                case RUDDER_BEHIND_STERN:
                    return 1.5f;
                case SKEG:
                    return 2.0f;
                case SHAFTS:
                    return 4.0f;
                default:
                    return GetMinAppendageResisstanceFactor();
            }
        }

        public float GetSurfaceArea()
        {
            switch (appendageType)
            {
                case SHAFTS:
                    return Mathf.PI * size.x * size.z;
                default:
                    return (size.x * size.y + size.x * size.z + size.y * size.z) * 2.0f;
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public string[] GetAppendageTypes() => new [] {
            "Rudder behind Skeg",
            "Rudder behind Stern",
            "Twin-Screw Balance Rudders",
            "Shaft Brackets",
            "Skeg",
            "Strut Bossings",
            "Hull Bossings",
            "Shafts",
            "Stabilizer Fins",
            "Dome",
            "Bilge Keel",
        };

        private void OnDrawGizmosSelected()
        {
            try {
                this.UpdateProxy();

                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
#endif
    }
}
