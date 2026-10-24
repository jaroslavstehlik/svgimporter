

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
