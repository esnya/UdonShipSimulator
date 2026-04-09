
using UdonSharp;
using UnityEngine;

namespace UdonShipSimulator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public partial class MaterialPropertyBlockWriter : UdonSharpBehaviour
    {
        public bool onStart;

        public bool writeColors;
        public Renderer[] colorTargets = { };
        public int[] colorIndices = { };
        public string[] colorNames = { "_Color" };
        public Color[] colorValues = { };

        public bool writeFloats;
        public Renderer[] floatTargets = { };
        public int[] floatIndices = { };
        public string[] floatNames = { };
        public float[] floatValues = { };

        public bool writeTextures;
        public Renderer[] textureTargets = { };
        public int[] textureIndices = { };
        public string[] textureNames = { };
        public Texture[] textureValues = { };

        private void Start()
        {
            if (onStart) Trigger();
        }

        public void Trigger()
        {
            var block = new MaterialPropertyBlock();

            if (writeColors)
            {
                for (int i = 0; i < colorTargets.Length; i++)
                {
                    var target = colorTargets[i];
                    var materialIndex = colorIndices[i];
                    target.GetPropertyBlock(block, materialIndex);
                    block.SetColor(colorNames[i], colorValues[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }

            if (writeFloats)
            {
                for (int i = 0; i < floatTargets.Length; i++)
                {
                    var target = floatTargets[i];
                    var materialIndex = floatIndices[i];
                    target.GetPropertyBlock(block, materialIndex);
                    block.SetFloat(floatNames[i], floatValues[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }

            if (writeTextures)
            {
                for (int i = 0; i < textureTargets.Length; i++)
                {
                    var target = textureTargets[i];
                    var materialIndex = textureIndices[i];
                    target.GetPropertyBlock(block, materialIndex);
                    block.SetTexture(textureNames[i], textureValues[i]);
                    target.SetPropertyBlock(block, materialIndex);
                }
            }
        }

    }
}
