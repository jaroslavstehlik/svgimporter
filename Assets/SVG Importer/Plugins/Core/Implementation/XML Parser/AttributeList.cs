// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;

namespace SVGImporter.Document
{
    public struct AttributeList
    {
        Dictionary<string,string> attrs;

        public AttributeList(AttributeList a)
        {
            if (a.attrs != null)
                attrs = new Dictionary<string,string>(a.attrs);
            else
                attrs = null;
        }

        public void Clear()
        {
            if (attrs != null)
                attrs.Clear();
        }

        public void Add(string name, string value)
        {
            if (attrs == null)
                attrs = new Dictionary<string,string>();
            attrs [name] = value;
        }

        public string GetValue(string name)
        {
            string outVal;
            if ((attrs != null) && attrs.TryGetValue(name, out outVal))
                return outVal;
            else
                return "";
        }

        public new string ToString()
        {
            if (attrs == null)
                return "null";
            else
            {
                bool isFirst = true;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (KeyValuePair<string,string> kvp in attrs)
                {
                    if (!isFirst)
                        sb.Append(", ");
                    sb.Append(kvp.Key).Append("=").Append(kvp.Value);
                    isFirst = false;
                }
                return sb.ToString();
            }
        }

        public int Count
        {
            get {
                return attrs.Count;
            }
        }

        public Dictionary<string,string> Get
        {
            get {
                return attrs;
            }
        }
        
        public Dictionary<string,string> Set
        {
            set {
                attrs = value;
            }
        }
    }
}
