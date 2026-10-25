

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter 
{
    using Rendering;
    using Utils;
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(ISVGRenderer))]
    [AddComponentMenu("Rendering/SVG Modifiers/Blur Modifier", 22)]
    public class SVGBlurModifier : SVGModifier 
    {
    // Unity 6.0 still has the obsolete Component.camera, which this field hides (CS0108);
    // later versions removed it, where `new` would warn instead (CS0109).
#pragma warning disable CS0108, CS0109
    public Camera camera;
#pragma warning restore CS0108, CS0109
        public bool useCameraVelocity;

        public float radius = 20f;
        public bool motionBlur = false;
        public bool manualMotionBlur = true;
        public float direction = 0f;

        protected Vector3 lastPosition;
        protected Vector2 transformVelocity;

        protected Camera mainCamera
        {
            get {
                if(camera == null)
                {
                    if(Camera.current != null)
                    {
                        return Camera.current;
                    } else {
                        return Camera.main;
                    }
                }
                return camera;
            }
        }

        void LateUpdate()
        {
            transformVelocity = (Vector2)(transform.position - lastPosition);
            if(Time.deltaTime > 0f)
            {
                transformVelocity.x /= Time.deltaTime;
                transformVelocity.y /= Time.deltaTime;
            }
            lastPosition = transform.position;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            lastPosition = transform.position;
        }

        // Widens an antialiasing fringe outward: its outer, transparent edge moves out by the blur,
        // while its inner edge stays on the shape's outline, so the shape keeps its solid core. A
        // unit of extrusion moves a vertex half a pixel (two in 1.x), so the added spread is
        // doubled to keep the radius's meaning.
        static void Spread(SVGShape fringe, SVGMatrix matrix)
        {
            if(fringe.angles == null || fringe.colors == null || fringe.colors.Length != fringe.angles.Length) return;
            for(int k = 0; k < fringe.angles.Length; k++)
            {
                if(fringe.colors[k].a != 0) continue;
                Vector2 extrusion = fringe.angles[k];
                fringe.angles[k] = matrix.Transform(extrusion) * 2f - extrusion;
            }
        }

        protected override void PrepareForRendering (SVGLayer[] layers, SVGAsset svgAsset, bool force) 
        {
            if (!active) return;
            if (layers == null) return;
            Camera currentCamera = mainCamera;

            SVGMatrix scaleMatrix = SVGMatrix.identity;
            SVGMatrix rotationMatrix = SVGMatrix.identity;

            //Matrix4x4 M = transform.localToWorldMatrix;
            Matrix4x4 V = currentCamera.worldToCameraMatrix;
            Matrix4x4 P = currentCamera.projectionMatrix;
            Matrix4x4 PV = P * V;

            float tempRadius = radius;
            float screenSize = ((Vector2)PV.MultiplyVector(Vector2.one * radius)).magnitude;
            if(currentCamera.orthographic)
            {
                tempRadius *= screenSize;
            } else
            {
                float camDistance = Vector3.Distance(transform.position, currentCamera.transform.position);
                if(camDistance > 0f)
                {
                    tempRadius *= screenSize / camDistance;
                } else {
                    tempRadius *= screenSize;
                }
            }

            if(!motionBlur)
            {
                scaleMatrix = scaleMatrix.Scale(tempRadius);
            } else
            {
                float intensity = tempRadius;

                if(!manualMotionBlur)
                {
                    Vector2 localVelocity = transformVelocity;
                    if(useCameraVelocity)
                    {
                        localVelocity += (Vector2)transform.InverseTransformVector(currentCamera.velocity);
                    }

                    float magnitude = Mathf.Sqrt(localVelocity.x * localVelocity.x + localVelocity.y * localVelocity.y);
                    Vector2 velocityNormalized = Vector2.zero;
                    if(magnitude > 0f)
                    {
                        velocityNormalized.x = localVelocity.x / magnitude;
                        velocityNormalized.y = localVelocity.y / magnitude;
                    }

                    direction = Mathf.Atan2(velocityNormalized.y, velocityNormalized.x) * Mathf.Rad2Deg;
                    intensity = magnitude * tempRadius;
                }

                scaleMatrix = scaleMatrix.Scale(1f + intensity, 1f);
            }

            rotationMatrix = rotationMatrix.Rotate(-direction);
            SVGMatrix rotationMatrixInverse = SVGMatrix.identity;
            rotationMatrixInverse = rotationMatrixInverse.Rotate(direction);
            SVGMatrix matrix = rotationMatrixInverse.Multiply(scaleMatrix.Multiply(rotationMatrix));

            int totalLayers = layers.Length;
            if(!useSelection)
            {
                for(int i = 0; i < totalLayers; i++)
                {
                    if(layers[i].shapes == null) continue;
                    int shapesLength = layers[i].shapes.Length;
                    for(int j = 0; j < shapesLength; j++)
                    {
                        if(layers[i].shapes[j].type != SVGShapeType.ANTIALIASING) continue;
                        Spread(layers[i].shapes[j], matrix);
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
                            if (layers[layerIndex].shapes[j].type != SVGShapeType.ANTIALIASING) continue;
                            Spread(layers[layerIndex].shapes[j], matrix);
                        }
                    }
                }
            }
        }
    }
}
