// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SVGImporter.Utils
{
    using Rendering;

    public static class SVGStringExtractor
    {        
        public static string pathCommands = "ZzMmLlCcSsQqTtAaHhVv";

        //--------------------------------------------------
        //Extract for Syntax:   translate(700 200)rotate(-30)
        private static char[] splitPipe = new char[] { ')' };

        public static List<SVGTransform> ExtractTransformList(string inputText)
        {
            List<SVGTransform> _return = new List<SVGTransform>();

            string[] valuesStr = inputText.Split(splitPipe, StringSplitOptions.RemoveEmptyEntries);

            int len = valuesStr.Length;
            for (int i = 0; i < len; i++)
            {
                if(string.IsNullOrEmpty(valuesStr [i]))
                    continue;

                int vt1 = valuesStr [i].IndexOf('(');
                if(vt1 <= 0) continue;

                string _key = valuesStr [i].Substring(0, vt1).Trim();
                string _value = valuesStr [i].Substring(vt1 + 1).Trim();
                if(!string.IsNullOrEmpty(_key))
                {
                    _return.Add(new SVGTransform(_key, _value));
                }
            }
            return _return;
        }
        //--------------------------------------------------
        //Extract for Syntax:  700 200 -30
        public static char[] splitSpaceComma = new char[]
        {
            ' ',
            ',',
            '\n',
            '\t',
            '\r'
        };

        public static float[] ExtractTransformValueAsPX(string inputText)
        {
            string[] tmp = ExtractTransformValue(inputText);
            float[] values = new float[tmp.Length];
            for (int i = 0; i < values.Length; i++)
                values [i] = SVGLength.GetPXLength(tmp [i]);
            return values;
        }

        public static string[] ExtractTransformValue(string inputText)
        {
            if(inputText.Length > 1)
            {
                for(int i = 1; i < inputText.Length; i++)
                {
                    if(inputText[i] == '-' && inputText[i - 1] != 'e')
                    {
                        inputText = inputText.Insert(i++, " ");
                    }
                }
            }

            char[] splitDecimalPoint = new char[]{'.'};
            List<string> output = new List<string>(inputText.Split(splitSpaceComma, StringSplitOptions.RemoveEmptyEntries));
            for(int i = 0; i < output.Count; i++)
            {             
                if(output[i][0] == splitDecimalPoint[0])
                {
                    output[i] = output[i].Insert(0, "0");
                }
                string[] temp = output[i].Split(splitDecimalPoint, StringSplitOptions.RemoveEmptyEntries);
                int tempLength = temp.Length;

                if(tempLength > 2)
                {
                    output[i] = temp[0] +"."+ temp[1];
                    for(int j = 2; j < tempLength; j++)
                    {
                        output.Insert(++i, "0."+temp[j]);
                    }
                }
            }
            return output.ToArray();
        }
        //--------------------------------------------------
        //Extract for Systax : M100 100 C200 100,...
        private static List<int> _break = new List<int>();
        // WARNING:  This method is NOT thread-safe due to use of static _break member!
        public static void ExtractPathSegList(string inputText, ref List<char> charList, ref List<string> valueList)
        {
            _break.Clear();
            for (int i = 0; i < inputText.Length; i++)
            {
                if (pathCommands.IndexOf(inputText[i]) >= 0)
                {
                    _break.Add(i);
                }
            }

            _break.Add(inputText.Length);
            charList.Capacity = _break.Count - 1;
            valueList.Capacity = _break.Count - 1;

            for (int i = 0; i < _break.Count - 1; i++)
            {
                int _breakSpot1 = _break [i];
                int _breakSpot2 = _break [i + 1];
                string _string = inputText.Substring(_breakSpot1 + 1, _breakSpot2 - _breakSpot1 - 1);
                charList.Add(inputText [_breakSpot1]);
                valueList.Add(_string);
            }
        }

        //--------------------------------------------------
        //Extract for Syntax:  fill: #ffffff; stroke:#000000; stroke-width:0.172
        private static char[] splitColonSemicolon = new char[]
        {
            ':',
            ';',
            ' ',
            '\n',
            '\t',
            '\r'
        };

        public static string[] ExtractStringArray(string inputText)
        {
            return inputText.Split(splitSpaceComma, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void ExtractStyleValue(string inputText, ref Dictionary<string, string> dic)
        {
            string[] valuesStr = inputText.Split(splitColonSemicolon, StringSplitOptions.RemoveEmptyEntries);

            int len = valuesStr.Length - 1;
            for (int i = 0; i < len; i += 2)
                dic.Add(valuesStr [i], valuesStr [i + 1]);
        }
        //--------------------------------------------------
        //Extract for Syntax:   url(#identifier)
        public static string ExtractUrl(string inputText)
        {
            string _return = "";

            inputText = inputText
          .Replace('\n', ' ')
          .Replace('\t', ' ')
          .Replace('\r', ' ')
          .Replace(" ", "");

            int vt1 = inputText.IndexOf("url(#"),
            vt2 = inputText.IndexOf(")");
            if (vt2 < 0)
                vt2 = inputText.Length;

            _return = inputText.Substring(vt1 + 5, vt2 - vt1 - 5);

            return _return;
        }
    }
}
