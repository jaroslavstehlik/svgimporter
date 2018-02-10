// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Data
{
    using Geometry;
    using Utils;

    public class SVGDepthTree : System.Object {

        protected QuadTree<int> quadTree;

        public SVGDepthTree(SVGBounds bounds)
        {
            quadTree = new QuadTree<int>(new SVGBounds(bounds.center, bounds.size));
        }

        public SVGDepthTree(Rect bounds)
        {
            quadTree = new QuadTree<int>(new SVGBounds(bounds.center, bounds.size));
        }

        public int[] TestDepthAdd(int node, SVGBounds bounds)
        {
            List<QuadTreeNode<int>> overlapNodes = quadTree.Intersects(bounds);
            int[] output = null;
            if(overlapNodes != null && overlapNodes.Count > 0)
            {
                output = new int[overlapNodes.Count];
                for(int i = 0 ; i < output.Length; i++)
                {
                    output[i] = overlapNodes[i].data;
                }
            }

            quadTree.Add(node, bounds);
            return output;
        }

        public void Clear()
        {
            quadTree.Clear();
        }
    }
}