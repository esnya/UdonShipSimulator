
using UdonSharp;
using UdonToolkit;
using UnityEngine;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace UdonShipSimulator
{
    public class MaterialPropertyBlockWriter : UdonSharpBehaviour
    {
        public bool onStart;

        [Space, SectionHeader("Color")]
        public bool writeColors;
        [ListView("Color")] public Renderer[] colorTargets = {};
        [ListView("Color")] public int[] colorIndices = {};
        [ListView("Color")] public string[] colorNames = { "_Color" };
        [ListView("Color")] public Color[] colorValues = {};

        private int colorTargetCount;
        private void Start()
        {
            colorTargetCount = Mathf.Min(Mathf.Min(colorTargets.Length, colorIndices.Length), colorValues.Length);

            if (onStart) Trigger();
        }

        public void Trigger()
        {
            if (writeColors)
            {
                for (int i = 0; i < colorTargetCount; i++)
                {
                    var block = new MaterialPropertyBlock();
                    var target = colorTargets[i];
                    var materialIndex = colorIndices[i];
                    target.GetPropertyBlock(block, materialIndex);
                    block.SetColor(colorNames[i], colorValues[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [Button("Fill Color With First", true)]
        public void FillColorWithFirst()
        {
            this.UpdateProxy();
            for (int i = 1; i < colorValues.Length; i++)
            {
                colorValues[i] = colorValues[0];
            }
            this.ApplyProxyModifications();
        }
#endif
    }
}
