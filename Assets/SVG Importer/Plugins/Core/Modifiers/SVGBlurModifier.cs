

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
    new public Camera camera;
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
                        if(layers[i].shapes[j].angles == null) continue;
                        int vertexCount = layers[i].shapes[j].vertexCount;
                        for(int k = 0; k < vertexCount; k++)
                        {
                            Vector2 dir = layers[i].shapes[j].angles[k];                           
                            dir = matrix.Transform(dir);
                            layers[i].shapes[j].angles[k] = dir;
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
                            if (layers[layerIndex].shapes[j].type != SVGShapeType.ANTIALIASING) continue;
                            if (layers[layerIndex].shapes[j].angles == null) continue;

                            for (int k = 0; k < vertexCount; k++)
                            {
                                Vector2 dir = layers[layerIndex].shapes[j].angles[k];
                                dir = matrix.Transform(dir);
                                layers[layerIndex].shapes[j].angles[k] = dir;
                            }
                        }
                    }
                }
            }
        }
    }
}
