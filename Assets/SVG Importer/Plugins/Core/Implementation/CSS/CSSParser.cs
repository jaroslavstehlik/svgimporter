// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
