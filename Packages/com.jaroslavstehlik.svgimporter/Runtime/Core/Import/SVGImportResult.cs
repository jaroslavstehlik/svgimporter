// Copyright (C) 2026 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SVGImporter
{
    using Rendering;

    /// <summary>Everything an import produced, before it becomes an SVG Asset.</summary>
    internal class SVGImportResult
    {
        /// <summary>Whether the document was imported. False: only errors and messages are set.</summary>
        public bool succeeded;
        /// <summary>The mesh; null when importing layers (options.useLayers) or when the import failed.</summary>
        public Mesh mesh;
        /// <summary>The shader of each submesh, by name.</summary>
        public string[] shaders;
        /// <summary>The gradients the mesh refers to by index; null without gradients.</summary>
        public SVGGradient[] gradients;
        /// <summary>The layers, with options.useLayers.</summary>
        public SVGLayer[] layers;
        /// <summary>The outline for a 2D collider, with options.generateCollider.</summary>
        public SVGPath[] colliderShape;
        /// <summary>The SVG canvas in mesh units.</summary>
        public Rect canvasRectangle;
        /// <summary>Unsupported features, and errors.</summary>
        public List<SVGError> errors = new List<SVGError>();
        /// <summary>Details for the errors and warnings, for the console.</summary>
        public List<string> messages = new List<string>();
        /// <summary>A hash of the SVG text and the options: equal for identical imports.</summary>
        public long importHash;

        /// <summary>Whether the document couldn't be read.</summary>
        public bool hasCriticalErrors
        {
            get { return errors.Any(e => e == SVGError.Syntax || e == SVGError.CorruptedFile); }
        }
    }

    /// <summary>What the engines produce for the shared steps of an import.</summary>
    internal class SVGImportContext
    {
        public SVGAtlasData atlasData;
        public readonly List<SVGLayer> layers = new List<SVGLayer>();
        public readonly List<SVGPath> paths = new List<SVGPath>();
        public readonly List<SVGError> errors;
        public readonly List<string> messages;

        public SVGImportContext(List<SVGError> errors, List<string> messages)
        {
            this.errors = errors;
            this.messages = messages;
            Reset();
        }

        /// <summary>Starts over, for example when the import falls back to another engine.</summary>
        public void Reset()
        {
            layers.Clear();
            paths.Clear();
            atlasData = new SVGAtlasData();
            atlasData.Init(SVGAtlas.defaultAtlasTextureWidth * SVGAtlas.defaultAtlasTextureHeight);
            atlasData.AddGradient(SVGAtlasData.GetDefaultGradient());
        }
    }
}
