// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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