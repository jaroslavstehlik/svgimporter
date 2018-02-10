// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace SVGImporter.Utils
{        
    public class SVGDebugPoints : MonoBehaviour
    {
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            RenderGizmos();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
            RenderGizmos();
        }

        void RenderGizmos()
        {
            int count = transform.childCount;
            for(int i = 0; i < count; i++)
            {
                Vector3 position = transform.GetChild(i).localPosition;
                Gizmos.DrawSphere(position, UnityEditor.HandleUtility.GetHandleSize(position) * 0.1f);
            }
            
            if(count > 1)
            {
                Vector3 lastPosition = transform.GetChild(0).transform.localPosition;
                for(int i = 1; i < count; i++)
                {
                    Vector3 currentPosition = transform.GetChild(i).localPosition;
                    Gizmos.DrawLine(lastPosition, currentPosition);
                    lastPosition = currentPosition;
                }
            }
        }
#endif
    }
}
