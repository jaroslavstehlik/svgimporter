// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SVGImporter.Utils
{
    public enum CSSSelector
    {
        None,
        Element,
        Id,
        Class
    }

    public class CSSParser {

        const char elementStartChar = '{';
        const char elementEndChar = '}';
        const char elementSplitChar = ',';
        const char attributeStartChar = ':';
        const char attributeEndChar = ';';

        public static CSSSelector GetSelector(string value)
        {
            if(string.IsNullOrEmpty(value)) return CSSSelector.None;

            if(value[0] == '.')
            {
                return CSSSelector.Class;
            } else if(value[0] == '#')
            {
                return CSSSelector.Id;
            } else {
                return CSSSelector.Element;
            }
        }

        public static string CleanCSS(string cssString)
        {
            // Clear comments
            cssString = Regex.Replace(cssString, @"/\*.+?\*/", string.Empty, RegexOptions.Singleline);
            // Clear empty spaces
            cssString = Regex.Replace(cssString, @"\s+", "");

            return cssString;
        }

        public static Dictionary<string, Dictionary<string, string>> Parse(string value)
        {
            if(string.IsNullOrEmpty(value))
                return null;

            string[] elements = value.Split(elementEndChar);
            int i, j, attributesLength;

            Dictionary<string, Dictionary<string, string>> elementDictionary = new Dictionary<string, Dictionary<string, string>>();

            for(i = 0; i < elements.Length; i++)
            {
                if(string.IsNullOrEmpty(elements[i])) continue;

                string[] element = elements[i].Split(new char[]{elementStartChar}, System.StringSplitOptions.RemoveEmptyEntries);
                if(element == null || element.Length != 2) continue;

                Dictionary<string, string> attributeDictionary = new Dictionary<string, string>();
                
                string[] attributes = element[1].Split(new char[]{attributeEndChar}, System.StringSplitOptions.RemoveEmptyEntries);
                attributesLength = attributes.Length;
                for(j = 0; j < attributesLength; j++)
                {
                    if(string.IsNullOrEmpty(attributes[j])) continue;
                    string[] attribute = attributes[j].Split(new char[]{attributeStartChar}, System.StringSplitOptions.RemoveEmptyEntries);
                    if(attribute == null || attribute.Length != 2) continue;

                    if(attributeDictionary.ContainsKey(attribute[0]))
                    {
                        attributeDictionary[attribute[0]] = attribute[1];
                    } else {
                        attributeDictionary.Add(attribute[0], attribute[1]);
                    }
                }

                if(attributeDictionary.Count == 0)
                    continue;

                string[] elementIDs = element[0].Split(elementSplitChar);
                for(j = 0; j < elementIDs.Length; j++)
                {
                    if(string.IsNullOrEmpty(elementIDs[j])) continue;

                    if(elementDictionary.ContainsKey(elementIDs[j]))
                    {
                        elementDictionary[elementIDs[j]] = attributeDictionary;
                    } else {
                        elementDictionary.Add(elementIDs[j], attributeDictionary);
                    }
                }
            }

            if(elementDictionary.Count == 0)
                return null;

            return elementDictionary;
        }

    }
}
