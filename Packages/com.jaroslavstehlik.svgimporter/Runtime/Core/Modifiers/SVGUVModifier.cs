

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;

    [ExecuteInEditMode]
    [RequireComponent(typeof(ISVGShape), typeof(ISVGRenderer))]
    [AddComponentMenu("Rendering/SVG Modifiers/UV Modifier", 22)]
    public class SVGUVModifier : SVGModifier {

        public enum TransformOrder
        {
            TRS,
            TSR,
            RTS,
            RST,
            STR,
            SRT
        }

        //public SVGTransform2D svgTransform;
        public Vector2 position;
        public float rotation;
        public Vector2 scale = Vector2.one;
        public bool preprocess = true;
        public TransformOrder transformOrder = TransformOrder.TRS;

        protected override void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) 
        {
            if (!active) return;

            SVGMatrix T = SVGMatrix.identity.Translate(-position);
            SVGMatrix R = SVGMatrix.identity.Rotate(rotation);
            SVGMatrix S = SVGMatrix.identity.Scale(scale);

            SVGMatrix tempMatrix = SVGMatrix.identity;
            if(preprocess)
            {
                tempMatrix = tempMatrix.Translate(Vector2.one * 0.5f).Scale(0.25f, 0.25f);
            }

            switch(transformOrder)
            {
                case TransformOrder.TRS:
                    tempMatrix *= S * R * T;
                    break;
                case TransformOrder.TSR:
                    tempMatrix *= R * S * T;
                break;
                case TransformOrder.RTS:
                    tempMatrix *= S * T * R;
                break;
                case TransformOrder.RST:
                    tempMatrix *= T * S * R;
                break;
                case TransformOrder.STR:
                    tempMatrix *= S * T * S;
                break;
                case TransformOrder.SRT:
                    tempMatrix *= T * R * S;
                break;
            }

            if(layers == null) return;
            int totalLayers = layers.Length;
            if(!useSelection)
            {
                for(int i = 0; i < totalLayers; i++)
                {
                    if(layers[i].shapes == null) continue;
                    int shapesLength = layers[i].shapes.Length;
                    for(int j = 0; j < shapesLength; j++)
                    {
                        int vertexCount = layers[i].shapes[j].vertexCount;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            layers[i].shapes[j].fill.fillType = FILL_TYPE.TEXTURE;
                            layers[i].shapes[j].fill.transform = tempMatrix;
                        }
                    }
                }
            } else
            {
                if (layerSelection.layers != null)
                {
                    int selectionCount = layerSelection.layers.Count;
                    for (int i = 0; i < selectionCount; i++)
                    {
                        int layerIndex = layerSelection.layers[i];
                        if (layerIndex < 0 || layerIndex >= totalLayers) continue;
                        if (layers[layerIndex].shapes == null) continue;
                        int shapesLength = layers[layerIndex].shapes.Length;
                        for (int j = 0; j < shapesLength; j++)
                        {
                            int vertexCount = layers[layerIndex].shapes[j].vertexCount;
                            layers[layerIndex].shapes[j].fill.fillType = FILL_TYPE.TEXTURE;
                            layers[layerIndex].shapes[j].fill.transform = tempMatrix;
                        }
                    }
                }
            }
        }
    }
}
