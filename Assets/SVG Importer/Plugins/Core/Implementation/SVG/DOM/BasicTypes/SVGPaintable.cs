// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Rendering 
{    
    using Utils;
    using Document;

    public enum SVGOverflow
    {
        visible,
        hidden,
        scroll,
        auto
    }

    public enum SVGClipPathUnits
    {
        UserSpaceOnUse,
        ObjectBoundingBox
    }

    public enum SVGClipRule
    {
        nonzero,
        evenodd
    }

    public enum SVGVisibility
    {
        Visible,
        Hidden,
        Collapse
    }

    public enum SVGDisplay
    {
        Inline,
        Block,
        Flex,
        InlineBlock,
        InlineFlex,
        InlineTable,
        ListItem,
        RunIn,
        Table,
        TableCaption,
        TableColumnGroup,
        TableHeaderGroup,
        TableFooterGroup,
        TableRowGroup,
        TableCell,
        TableColumn,
        TableRow,
        None
    }

    public enum SVGFillRule
    {
        NonZero,
        EvenOdd
    }

    public enum SVGStrokeLineCapMethod
    {
        Unknown,
        Butt,
        Round,
        Square
    }

    public enum SVGStrokeLineJoinMethod
    {
        Unknown,
        Miter,
        MiterClip,
        Round,
        Bevel
    }

    public enum SVGPaintMethod
    {
        SolidFill,
        LinearGradientFill,
        RadialGradientFill,
        ConicalGradientFill,
        PathDraw,
        NoDraw
    }

    [System.Serializable]
    public class SVGPaintable
    {
        private Rect _viewport;
        public Rect viewport
        {
            get {
                return _viewport;
            }
        }

        private SVGVisibility _visibility;
        private SVGDisplay _display;
        private SVGOverflow _overflow;
        private SVGClipPathUnits _clipPathUnits;
        private SVGClipRule _clipRule;
        private float _opacity;
        private float _fillOpacity;
        private float _strokeOpacity;
        private SVGColor? _fillColor;
        private SVGColor? _strokeColor;
        private SVGLength _strokeWidth;
        private SVGLength _miterLimit;
        private float[] _dashArray;
        private SVGLength _dashOfset;

        private bool isStrokeWidth = false;
        private SVGStrokeLineCapMethod _strokeLineCap = SVGStrokeLineCapMethod.Unknown;
        private SVGStrokeLineJoinMethod _strokeLineJoin = SVGStrokeLineJoinMethod.Unknown;
        private SVGFillRule _fillRule = SVGFillRule.NonZero;

        private Dictionary<string, Dictionary<string, string>> _cssStyle;
        private List<List<Vector2>> _clipPathList;
        private Dictionary<string, SVGLinearGradientElement> _linearGradList;
        private Dictionary<string, SVGRadialGradientElement> _radialGradList;
        private Dictionary<string, SVGConicalGradientElement> _conicalGradList;

        private string _gradientID = "";

        public SVGVisibility visibility
        {
            get { return _visibility; }
        }

        public SVGDisplay display
        {
            get { return _display; }
        }

        public SVGOverflow overflow
        {
            get { return _overflow; }
        }

        public SVGClipPathUnits clipPathUnits
        {
            get { return _clipPathUnits; }
        }

        public SVGClipRule clipRule
        {
            get { return _clipRule; }
        }

        public SVGFill svgFill;
        
        public SVGColor? fillColor
        {
            get { return this._fillColor; }
        }

        public SVGColor? strokeColor
        {
            get {
                if (IsStroke())
                    return this._strokeColor;
                else
                    return null;
            }
        }

        public float opacity
        {
            get { return this._opacity; }
        }

        public float fillOpacity
        {
            get { return this._fillOpacity; }
        }

        public float strokeOpacity
        {
            get { return this._strokeOpacity; }
        }

        public float strokeWidth
        {
            get { return this._strokeWidth.value; }
        }

        public float miterLimit
        {
            get { return this._miterLimit.value; }
        }

        public float[] dashArray
        {
            get { return this._dashArray; }
        }

        public float dashOffset
        {
            get { return this._dashOfset.value; }
        }

        public SVGStrokeLineCapMethod strokeLineCap
        {
            get { return this._strokeLineCap; }
        }

        public SVGStrokeLineJoinMethod strokeLineJoin
        {
            get { return this._strokeLineJoin; }
        }

        public SVGFillRule fillRule
        {
            get { return this._fillRule; }
        }

        public Dictionary<string, Dictionary<string, string>> cssStyle
        {
            get { return this._cssStyle; }
        }

        public List<List<Vector2>> clipPathList
        {
            get { return this._clipPathList; }
        }

        public Dictionary<string, SVGLinearGradientElement> linearGradList
        {
            get { return this._linearGradList; }
        }

        public Dictionary<string, SVGRadialGradientElement> radialGradList
        {
            get { return this._radialGradList; }
        }

        public Dictionary<string, SVGConicalGradientElement> conicalGradList
        {
            get { return this._conicalGradList; }
        }

        public string gradientID
        {
            get { return this._gradientID; }
        }

        void InitDefaults()
        {
            isStrokeWidth = false;

            this._visibility = SVGVisibility.Visible;
            this._display = SVGDisplay.Inline;
            this._overflow = SVGOverflow.visible;
            this._clipPathUnits = SVGClipPathUnits.UserSpaceOnUse;
            this._clipRule = SVGClipRule.nonzero;
            this._opacity = 1f;
            this._fillOpacity = 1f;
            this._strokeOpacity = 1f;
            this._fillColor = new SVGColor();
            this._strokeColor = new SVGColor();
            this._strokeWidth = new SVGLength(1);
            this._strokeLineJoin = SVGStrokeLineJoinMethod.Miter;
            this._strokeLineCap = SVGStrokeLineCapMethod.Butt;
            this._fillRule = SVGFillRule.NonZero;
            this._miterLimit = new SVGLength(4);
            this._dashArray = null;
            this._dashOfset = new SVGLength(0);
            this._cssStyle = new Dictionary<string, Dictionary<string, string>>();
            this._clipPathList = new List<List<Vector2>>();
            this._linearGradList = new Dictionary<string, SVGLinearGradientElement>();
            this._radialGradList = new Dictionary<string, SVGRadialGradientElement>();
            this._conicalGradList = new Dictionary<string, SVGConicalGradientElement>();
        }

        public SVGPaintable()
        {
            InitDefaults();
        }

        public SVGPaintable(Node node)
        {
            InitDefaults();
            Initialize(node.attributes);
            ReadCSS(node);
        }

        public void AddCSS(string cssString)
        {
            if(string.IsNullOrEmpty(cssString)) return;
            Dictionary<string, Dictionary<string, string>> currentStyle = CSSParser.Parse(cssString);
            if(currentStyle == null || currentStyle.Count == 0) return;

            foreach(KeyValuePair<string, Dictionary<string, string>> element in currentStyle)
            {
                if(this._cssStyle.ContainsKey(element.Key))
                {
                    this._cssStyle[element.Key] = element.Value;
                } else {
                    this._cssStyle.Add(element.Key, element.Value);
                }
            }
        }

        List<List<Vector2>> CloneClipPathList(List<List<Vector2>> input)
        {
            if(input != null)
            {
                List<List<Vector2>> output = new List<List<Vector2>>();
                for(int i = 0; i < input.Count; i++)
                {
                    if(input[i] == null || input[i].Count == 0) continue;
                    output.Add(new List<Vector2>(input[i].ToArray()));
                }
                return output;
            }

            return null;
        }

        public SVGPaintable(SVGPaintable inheritPaintable, Node node)
        {
            InitDefaults();

            if(inheritPaintable != null)
            {
                this._visibility = inheritPaintable.visibility;
                this._display = inheritPaintable.display;
                this._clipRule = inheritPaintable.clipRule;
                this._viewport = inheritPaintable._viewport;           
                this._fillRule = inheritPaintable._fillRule;
                this._cssStyle = inheritPaintable._cssStyle;
                this._clipPathList = CloneClipPathList(inheritPaintable._clipPathList);                
                this._linearGradList = inheritPaintable._linearGradList;
                this._radialGradList = inheritPaintable._radialGradList;
                this._conicalGradList = inheritPaintable._conicalGradList;
            }

            if(inheritPaintable != null)
            {
                if (IsFillX() == false)
                {
                    if (inheritPaintable.IsLinearGradiantFill())
                    {
                        this._gradientID = inheritPaintable.gradientID;
                    } else if (inheritPaintable.IsRadialGradiantFill())
                    {
                        this._gradientID = inheritPaintable.gradientID;
                    } else
                        this._fillColor = inheritPaintable.fillColor;
                }

                if (!IsStroke() && inheritPaintable.IsStroke())
                {
                    this._strokeColor = inheritPaintable.strokeColor;
                }

                if (_strokeLineCap == SVGStrokeLineCapMethod.Unknown)
                {
                    _strokeLineCap = inheritPaintable.strokeLineCap;
                }

                if (_strokeLineJoin == SVGStrokeLineJoinMethod.Unknown)
                {
                    _strokeLineJoin = inheritPaintable.strokeLineJoin;
                }

                if (isStrokeWidth == false)
                    this._strokeWidth.NewValueSpecifiedUnits(inheritPaintable.strokeWidth);
            }

            Initialize(node.attributes);
            ReadCSS(node);

            if(inheritPaintable != null)
            {
                _opacity *= inheritPaintable._opacity;
                _fillOpacity *= inheritPaintable._fillOpacity;
                _strokeOpacity *= inheritPaintable._strokeOpacity;
            }
        }

        //style="fill: #ffffff; stroke:#000000; stroke-width:0.172"
        private void Initialize(AttributeList attrList)
        {         
            ReadStyle(attrList.Get);
            ReadStyle(attrList.GetValue("style"));
        }

        public void ReadCSS(Node node)
        {
            if(_cssStyle == null || _cssStyle.Count == 0) return;

            AttributeList attrList = node.attributes;

            string classString = attrList.GetValue("class");
            if(!string.IsNullOrEmpty(classString))
            {
                string[] classes = classString.Split(' ');
                for(int i = 0; i < classes.Length; i++)
                {
                    string className = "."+classes[i];
                    if(_cssStyle.ContainsKey(className))
                    {
                        ReadCSSElement(_cssStyle[className]);
                    }
                }
            }
        }

        public void ReadCSSElement(Dictionary<string, string> element)
        {
            if(element == null || element.Count == 0) return;
            ReadStyle(element);
        }

        public void SetViewport(Rect viewport)
        {
            this._viewport = viewport;
        }

        private void ReadStyle(string styleString)
        {
            if(string.IsNullOrEmpty(styleString)) return;
            Dictionary<string, string> _dictionary = new Dictionary<string, string>();
            SVGStringExtractor.ExtractStyleValue(styleString, ref _dictionary);
            ReadStyle(_dictionary);
        }

        private void ReadClipPath(string clipPathValue)
        {
            if (clipPathValue.IndexOf("url") >= 0)
            {
                string clipPathURL = SVGStringExtractor.ExtractUrl(clipPathValue);
                if(!string.IsNullOrEmpty(clipPathURL) && SVGParser._defs.ContainsKey(clipPathURL))
                {
                    Node clipPathNode = SVGParser._defs[clipPathURL];
                    if(clipPathNode != null)
                    {
                        SVGMatrix svgMatrix = SVGMatrix.identity;

                        string clipPathUnitsString = clipPathNode.attributes.GetValue("clipPathUnits");
                        switch(clipPathUnitsString.ToLower())
                        {
                            case "userSpaceOnUse":
                                _clipPathUnits = SVGClipPathUnits.UserSpaceOnUse;
                                break;
                            case "objectBoundingBox":
                                _clipPathUnits = SVGClipPathUnits.ObjectBoundingBox;
                                break;
                        }

                        List<Node> clipPathNodes = clipPathNode.GetNodes();
                        List<List<Vector2>> currentClipPathList = new List<List<Vector2>>();
                        if(clipPathNodes != null && clipPathNodes.Count > 0)
                        {
                            for(int i = 0; i < clipPathNodes.Count; i++)
                            {
                                List<List<Vector2>> clipPath = GetClipPath(clipPathNodes[i], svgMatrix);
                                if(clipPath != null)
                                {
                                    currentClipPathList.AddRange(clipPath);
                                }
                            }
                        }

                        if(currentClipPathList.Count > 0)
                        {
                            currentClipPathList = SVGGeom.MergePolygon(currentClipPathList);
                        }

                        if(_clipPathList != null && _clipPathList.Count > 0)
                        {
                            _clipPathList = SVGGeom.ClipPolygon(_clipPathList, currentClipPathList);
                        } else {
                            _clipPathList = currentClipPathList;
                        }
                    }
                }
            } 
        }
        
        private List<List<Vector2>> GetClipPath(Node node, SVGMatrix svgMatrix)
        {
            SVGTransformList transformList = new SVGTransformList();
            transformList.AppendItem(new SVGTransform(svgMatrix));

            switch (node.name)
            {
                case SVGNodeName.Rect:
                {
                    return new SVGRectElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Line:
                {
                    return new SVGLineElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Circle:
                {
                    return new SVGCircleElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Ellipse:
                {
                    return new SVGEllipseElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.PolyLine:
                {
                    return new SVGPolylineElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Polygon:
                {
                    return new SVGPolygonElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Path:
                {
                    return new SVGPathElement(node, transformList).GetClipPath();
                }
                case SVGNodeName.Use:
                {
                    string xlink = node.attributes.GetValue("xlink:href");
                    if (!string.IsNullOrEmpty(xlink))
                    {
                        if (xlink [0] == '#')
                            xlink = xlink.Remove(0, 1);
                        
                        if (SVGParser._defs.ContainsKey(xlink))
                        {
                            Node definitionNode = SVGParser._defs [xlink];
                            if (definitionNode != null && definitionNode != node)
                            {
                                return GetClipPath(definitionNode, svgMatrix);
                            }
                        }
                    }
                    break;
                }
            }

            return null;
        }

        private void ReadStyle(Dictionary<string, string> _dictionary)
        {
            if(_dictionary == null || _dictionary.Count == 0) return;

            if (_dictionary.ContainsKey("visibility"))
            {
                SetVisibility(_dictionary ["visibility"]);
            }
            if (_dictionary.ContainsKey("display"))
            {
                SetDisplay(_dictionary ["display"]);
            }
            if (_dictionary.ContainsKey("overflow"))
            {
                SetOverflow(_dictionary ["overflow"]);
            }
            if (_dictionary.ContainsKey("clip-rule"))
            {
                SetClipRule(_dictionary ["clip-rule"]);
            }
            if (_dictionary.ContainsKey("clip-path"))
            {
                ReadClipPath(_dictionary ["clip-path"]);
            }
            if (_dictionary.ContainsKey("fill"))
            {
                string fillValue = _dictionary ["fill"];
                if (fillValue.IndexOf("url") >= 0)
                {
                    _gradientID = SVGStringExtractor.ExtractUrl(fillValue);
                } else {
                    _fillColor = new SVGColor(_dictionary ["fill"]);
                }
            }
            if (_dictionary.ContainsKey("opacity"))
            {
                _opacity *= new SVGLength(_dictionary ["opacity"]).value;
            }
            if (_dictionary.ContainsKey("fill-opacity"))
            {
                _fillOpacity *= new SVGLength(_dictionary ["fill-opacity"]).value;
            }
            if (_dictionary.ContainsKey("stroke-opacity"))
            {
                _strokeOpacity *= new SVGLength(_dictionary ["stroke-opacity"]).value;
            }
            if(_dictionary.ContainsKey("fill-rule"))
            {
                SetFillRule(_dictionary["fill-rule"]);
            }
            if (_dictionary.ContainsKey("stroke"))
            {
                _strokeColor = new SVGColor(_dictionary ["stroke"]);
            }
            if (_dictionary.ContainsKey("stroke-width"))
            {
                this.isStrokeWidth = true;
                _strokeWidth = new SVGLength(_dictionary ["stroke-width"]);
            }
            if (_dictionary.ContainsKey("stroke-linecap"))
            {
                SetStrokeLineCap(_dictionary ["stroke-linecap"]);
            }
            if (_dictionary.ContainsKey("stroke-linejoin"))
            {
                SetStrokeLineJoin(_dictionary ["stroke-linejoin"]);
            }
            if (_dictionary.ContainsKey("stroke-miterlimit"))
            {
                _miterLimit = new SVGLength(_dictionary["stroke-miterlimit"]);
            }
            if (_dictionary.ContainsKey("stroke-dasharray"))
            {
                SetDashArray(_dictionary["stroke-dasharray"].Split(','));
            }
            if (_dictionary.ContainsKey("stroke-dashoffset"))
            {
                _dashOfset = new SVGLength(_dictionary["stroke-dashoffset"]);
            }
        }

        private void SetVisibility(string visibilityType)
        {
            switch(visibilityType)
            {
                case "visible":
                    _visibility = SVGVisibility.Visible;
                    break;
                case "hidden":
                    _visibility = SVGVisibility.Hidden;
                    break;
                case "collapse":
                    _visibility = SVGVisibility.Collapse;
                    break;
            }
        }

        private void SetOverflow(string overflowType)
        {
            switch(overflowType)
            {
                case "visible":
                    _overflow = SVGOverflow.visible;
                    break;
                case "auto":
                    _overflow = SVGOverflow.auto;
                    break;
                case "hidden":
                    _overflow = SVGOverflow.hidden;
                    break;
                case "scroll":
                    _overflow = SVGOverflow.scroll;
                    break;                
            }
        }

        private void SetClipRule(string clipRuleType)
        {
            switch(clipRuleType)
            {
                case "nonzero":
                    _clipRule = SVGClipRule.nonzero;
                    break;
                case "evenodd":
                    _clipRule = SVGClipRule.evenodd;
                    break;                            
            }
        }

        private void SetDisplay(string displayType)
        {
            if(_display == SVGDisplay.None) return;

            switch(displayType)
            {
                case "inline":
                    _display = SVGDisplay.Inline;;
                    break;
                case "block":
                    _display = SVGDisplay.Block;
                    break;
                case "flex":
                    _display = SVGDisplay.Flex;
                    break;
                case "inline-block":
                    _display = SVGDisplay.InlineBlock;
                    break;
                case "inline-flex":
                    _display = SVGDisplay.InlineFlex;
                    break;
                case "inline-table":
                    _display = SVGDisplay.InlineTable;
                    break;
                case "list-item":
                    _display = SVGDisplay.ListItem;
                    break;
                case "run-in":
                    _display = SVGDisplay.RunIn;
                    break;
                case "table":
                    _display = SVGDisplay.Table;
                    break;
                case "table-caption":
                    _display = SVGDisplay.TableCaption;
                    break;
                case "table-column-group":
                    _display = SVGDisplay.TableColumnGroup;
                    break;
                case "table-header-group":
                    _display = SVGDisplay.TableHeaderGroup;
                    break;
                case "table-footer-group":
                    _display = SVGDisplay.TableFooterGroup;
                    break;
                case "table-row-group":
                    _display = SVGDisplay.TableRowGroup;
                    break;
                case "table-cell":
                    _display = SVGDisplay.TableCell;
                    break;
                case "table-column":
                    _display = SVGDisplay.TableColumn;
                    break;
                case "table-row":
                    _display = SVGDisplay.TableRow;
                    break;
                case "none":
                    _display = SVGDisplay.None;
                    break;
            }
        }

        private void SetDashArray(string[] dashArrayType)
        {
            if(dashArrayType != null && dashArrayType.Length > 0)
            {
                _dashArray = new float[dashArrayType.Length];
                for(int i = 0; i < _dashArray.Length; i++)
                {
                    _dashArray[i] = new SVGLength(dashArrayType[i]).value;
                }
            }
        }

        private void SetFillRule(string fillRuleType)
        {
            switch(fillRuleType)
            {
                case "nonzero":
                    _fillRule = SVGFillRule.NonZero;
                    break;
                case "evenodd":
                    _fillRule = SVGFillRule.EvenOdd;
                    break;
            }
        }

        private void SetStrokeLineCap(string lineCapType)
        {
            switch (lineCapType)
            {
                case "butt":
                    _strokeLineCap = SVGStrokeLineCapMethod.Butt;
                    break;
                case "round":
                    _strokeLineCap = SVGStrokeLineCapMethod.Round;
                    break;
                case "square":
                    _strokeLineCap = SVGStrokeLineCapMethod.Square;
                    break;
            }
        }

        private void SetStrokeLineJoin(string lineCapType)
        {
            switch (lineCapType)
            {
                case "miter":
                    _strokeLineJoin = SVGStrokeLineJoinMethod.Miter;
                    break;
                case "miter-clip":
                    _strokeLineJoin = SVGStrokeLineJoinMethod.MiterClip;
                    break;
                case "round":
                    _strokeLineJoin = SVGStrokeLineJoinMethod.Round;
                    break;
                case "bevel":
                    _strokeLineJoin = SVGStrokeLineJoinMethod.Bevel;
                    break;
            }
        }

        public bool IsLinearGradiantFill()
        {
            if (string.IsNullOrEmpty(this._gradientID))
            {
                return false;
            }
            return _linearGradList.ContainsKey(this._gradientID);
        }

        public bool IsRadialGradiantFill()
        {
            if (string.IsNullOrEmpty(this._gradientID))
            {
                return false;
            }
            return _radialGradList.ContainsKey(this._gradientID);
        }

        public bool IsConicalGradiantFill()
        {
            if (string.IsNullOrEmpty(this._gradientID))
            {
                return false;
            }            
            return _conicalGradList.ContainsKey(this._gradientID);
        }

        public bool IsSolidFill()
        {
            if (this._fillColor == null)
                return false;
            else
                return(this._fillColor.Value.colorType != SVGColorType.None);
        }

        public bool IsFill()
        {
            if (this._fillColor == null)
                return(IsLinearGradiantFill() || IsRadialGradiantFill());
            else
                return(this._fillColor.Value.colorType != SVGColorType.None);
        }

        public bool IsFillX()
        {
            if (this._fillColor == null)
                return(IsLinearGradiantFill() || IsRadialGradiantFill());
            else
                return(this._fillColor.Value.colorType != SVGColorType.Unknown);
        }

        public bool IsStroke()
        {
            if (this._strokeColor == null)
                return false;
            if ((this._strokeColor.Value.colorType == SVGColorType.Unknown) ||
                (this._strokeColor.Value.colorType == SVGColorType.None))
            {
                return false;
            }
            return true;
        }

        public SVGPaintMethod GetPaintType()
        {
            if (IsLinearGradiantFill())
            {
                return SVGPaintMethod.LinearGradientFill;
            }
            if (IsRadialGradiantFill())
            {
                return SVGPaintMethod.RadialGradientFill;
            }
            if (IsConicalGradiantFill())
            {
                return SVGPaintMethod.ConicalGradientFill;
            }
            if (IsSolidFill())
            {
                return SVGPaintMethod.SolidFill;
            }
            if (IsStroke())
            {
                return SVGPaintMethod.PathDraw;
            }

            return SVGPaintMethod.NoDraw;
        }

        public void AppendLinearGradient(SVGLinearGradientElement linearGradElement)
        {
            if(_linearGradList.ContainsKey(linearGradElement.id))
            {
                _linearGradList[linearGradElement.id] = linearGradElement;
            } else {
                _linearGradList.Add(linearGradElement.id, linearGradElement);
            }
        }

        public void AppendRadialGradient(SVGRadialGradientElement radialGradElement)
        {
            if(_radialGradList.ContainsKey(radialGradElement.id))
            {
                _radialGradList[radialGradElement.id] = radialGradElement;
            } else {
                _radialGradList.Add(radialGradElement.id, radialGradElement);
            }
        }
        
        public void AppendConicalGradient(SVGConicalGradientElement conicalGradElement)
        {
            if(_conicalGradList.ContainsKey(conicalGradElement.id))
            {
                _conicalGradList[conicalGradElement.id] = conicalGradElement;
            } else {
                _conicalGradList.Add(conicalGradElement.id, conicalGradElement);
            }
        }

        public SVGLinearGradientBrush GetLinearGradientBrush(Rect bounds, SVGMatrix matrix, Rect viewport)
        {
            if(!_linearGradList.ContainsKey(this._gradientID))
                return null;

            return new SVGLinearGradientBrush(_linearGradList[this._gradientID], bounds, matrix, viewport);
        }

        public SVGRadialGradientBrush GetRadialGradientBrush(Rect bounds, SVGMatrix matrix, Rect viewport)
        {
            if(!_radialGradList.ContainsKey(this._gradientID))
                return null;
            
            return new SVGRadialGradientBrush(_radialGradList[this._gradientID], bounds, matrix, viewport);
        }

        public SVGConicalGradientBrush GetConicalGradientBrush(Rect bounds, SVGMatrix matrix, Rect viewport)
        {
            if(!_conicalGradList.ContainsKey(this._gradientID))
                return null;
            
            return new SVGConicalGradientBrush(_conicalGradList[this._gradientID], bounds, matrix, viewport);
        }
    }
}
