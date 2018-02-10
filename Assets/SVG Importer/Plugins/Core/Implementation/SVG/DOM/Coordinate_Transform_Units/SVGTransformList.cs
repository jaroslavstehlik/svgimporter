// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;

namespace SVGImporter.Rendering
{
    using Document;
    using Utils;

    public class SVGTransformList
    {
        private List<SVGTransform> _listTransform;

        public int Count
        {
            get { return _listTransform.Count; }
        }

        public SVGMatrix totalMatrix
        {
            get
            {
                if (_listTransform.Count == 0)
                {
                    return SVGMatrix.identity;
                } else
                {
                    SVGMatrix matrix = _listTransform [0].matrix;
                    for (int i = 1; i < _listTransform.Count; i++)
                        matrix = matrix.Multiply(_listTransform [i].matrix);
                    return matrix;
                }
            }
        }

        public SVGTransformList()
        {
            _listTransform = new List<SVGTransform>();
        }

        public SVGTransformList(int capacity)
        {
            _listTransform = new List<SVGTransform>(capacity);
        }

        public SVGTransformList(string listString)
        {
            _listTransform = SVGStringExtractor.ExtractTransformList(listString);
        }

        public void Clear()
        {
            _listTransform.Clear();
        }

        public void AppendItem(SVGTransform newItem)
        {
            _listTransform.Add(newItem);
        }

        
        public void AppendItemAt(SVGTransform newItem, int index)
        {
            _listTransform.Insert(index, newItem);
        }

        public void AppendItems(SVGTransformList newListItem)
        {
            _listTransform.AddRange(newListItem._listTransform);
        }

        public void AppendItemsAt(SVGTransformList newListItem, int index)
        {
            _listTransform.InsertRange(index, newListItem._listTransform);
        }

        public SVGTransform this [int index]
        {
            get
            {
                if ((index < 0) || (index >= _listTransform.Count))
                    throw new DOMException(DOMExceptionType.IndexSizeErr);
                return _listTransform [index];
            }
        }

        public SVGTransform Consolidate()
        {
            return new SVGTransform(totalMatrix);
        }
    }
}
