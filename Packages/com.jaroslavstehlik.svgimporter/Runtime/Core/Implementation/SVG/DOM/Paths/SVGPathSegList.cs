

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
