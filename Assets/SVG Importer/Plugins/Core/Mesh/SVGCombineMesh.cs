// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Utils
{
    public class SVGCombineMesh {

        public static Mesh Combine(Mesh[] meshes)
        {
            CombineInstance[] combineInstances = new CombineInstance[meshes.Length];
            for(int i = 0; i < meshes.Length; i++)
            {
                combineInstances[i].mesh = meshes[i];
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combineInstances, false, false);
            return mesh;
        }   

    }
}
