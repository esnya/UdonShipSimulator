
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class MaterialPropertyBlockWriter : UdonSharpBehaviour
    {
        public bool onStart;

        [SectionHeader("Color")]
        public bool writeColors;
        [ListView("Color")] public Renderer[] colorTargets = {};
        [ListView("Color")] public int[] colorIndices = {};
        [ListView("Color")] public string[] colorNames = { "_Color" };
        [ListView("Color")] public Color[] colorValues = {};
        [ListView("Color")] public float[] colorIntensities = { 1.0f };

        [SectionHeader("Float")]
        public bool writeFloats;
        [ListView("Float")] public Renderer[] floatTargets = {};
        [ListView("Float")] public int[] floatIndices = {};
        [ListView("Float")] public string[] floatNames = {};
        [ListView("Float")] public float[] floatValues = {};

        private int colorTargetCount, floatTargetCount;
        private void Start()
        {
            colorTargetCount = Mathf.Min(Mathf.Min(Mathf.Min(colorTargets.Length, colorIndices.Length), colorNames.Length), Mathf.Min(colorValues.Length, colorIntensities.Length));
            floatTargetCount = Mathf.Min(Mathf.Min(floatTargets.Length, floatIndices.Length), Mathf.Min(floatNames.Length, floatValues.Length));

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
                    block.SetColor(colorNames[i], colorValues[i] * colorIntensities[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }

            if (writeFloats)
            {
                for (int i = 0; i < floatTargetCount; i++)
                {
                    var block = new MaterialPropertyBlock();
                    var target = floatTargets[i];
                    var materialIndex = floatIndices[i];
                    target.GetPropertyBlock(block, materialIndex);
                    block.SetFloat(floatNames[i], floatValues[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [Button("Apply Now", true)]
        public void _ApplyNow()
        {
            this.UpdateProxy();
            Start();
            if (!onStart) Trigger();
        }

        [Button("Apply All Of Scene", true)]
        public void _ApplyAllOfScene()
        {
            foreach (var udon in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<MaterialPropertyBlockWriter>()))
            {
                udon._ApplyNow();
            }
        }

        [Button("Set Renderer To This", true)]
        public void _SetRendererToThis()
        {
            this.UpdateProxy();
            for (int i = 0; i < colorTargets.Length; i++) colorTargets[i] = GetComponent<Renderer>();
            for (int i = 0; i < floatTargets.Length; i++) floatTargets[i] = GetComponent<Renderer>();
            this.ApplyProxyModifications();
        }

        [Button("Fill Color With First", true)]
        public void _FillColorWithFirst()
        {
            this.UpdateProxy();
            for (int i = 1; i < colorValues.Length; i++)
            {
                colorValues[i] = colorValues[0];
                colorIntensities[i] = colorIntensities[0];
            }
            this.ApplyProxyModifications();
        }
#endif
    }
}
