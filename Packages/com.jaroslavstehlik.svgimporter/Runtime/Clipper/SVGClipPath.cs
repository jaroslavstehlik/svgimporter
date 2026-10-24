using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    public class SVGClipPath : System.Object {

        public List<Vector2> path;
        public List<List<Vector2>> holes;

        public SVGClipPath ()
        {
        }

        public SVGClipPath (List<Vector2> path)
        {
            this.path = path;
        }

        public SVGClipPath (List<Vector2> path, List<List<Vector2>> holes)
        {
            this.path = path;
            this.holes = holes;
        }
    }
}