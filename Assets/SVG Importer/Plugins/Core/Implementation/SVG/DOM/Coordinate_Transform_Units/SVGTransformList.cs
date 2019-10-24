// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
