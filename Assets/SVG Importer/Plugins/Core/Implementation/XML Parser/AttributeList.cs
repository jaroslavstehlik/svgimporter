// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
