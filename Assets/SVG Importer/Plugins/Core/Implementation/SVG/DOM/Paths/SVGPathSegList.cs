// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections.Generic;

namespace SVGImporter.Rendering 
{
    public class SVGPathSegList
    {
        private List<object> _segList;

        public SVGPathSegList(int size)
        {
            _segList = new List<object>(size);
        }

        public int Count
        {
            get { return _segList.Count; }
        }

        public void Clear()
        {
            this._segList.Clear();
        }
        //-----------
        public SVGPathSeg GetItem(int index)
        {
            if (index < 0 || index >= _segList.Count)
            {
                return null;
            }
            return (SVGPathSeg)this._segList [index];        
        }
        public SVGPathSeg GetLastItem()
        {
            if(this._segList.Count == 0)
                return null;
            return (SVGPathSeg)this._segList[_segList.Count - 1];
        }
        //-----------
        public SVGPathSeg AppendItem(SVGPathSeg newItem)
        {
            if(newItem == null)
                return null;

            int segListCount = this._segList.Count;
            newItem.SetIndex(segListCount);
            //newItem.SetPreviousSegment(GetPreviousSegment(newItem.index));
            this._segList.Add(newItem);
            this.SetList(newItem);
            return newItem;
        }
        /***********************************************************************************/
        internal SVGPathSeg GetPreviousSegment(int index)
        {
            return this.GetItem(index - 1);
            /*
            int index = this._segList.IndexOf(seg);
            if (index <= 0)
            {
                return null;
            } else
            {
                return (SVGPathSeg)GetItem(index - 1);
            }
            */
        }
        /***********************************************************************************/
        private void SetList(SVGPathSeg newItem)
        {
            if (newItem != null)
                newItem.SetList(this);
        }
    }
}
