// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SVGImporter.Utils
{
    public static class SVGLengthConvertor
    {
        public static bool ExtractType(string text, ref float value, ref SVGLengthType lengthType)
        {
            string _value = "";
            int i;
            for (i = 0; i < text.Length; i++)
            {
                if ((('0' <= text [i]) && (text [i] <= '9')) || (text [i] == '+') || (text [i] == '-') || (text [i] == '.' ) || (text [i] == 'e'))
                {
                    _value = _value + text [i];
                } else if (text [i] == ' ')
                {
                    // Skip.
                } else
                {
                    break;
                }
            }
            string unit = text.Substring(i);
            if (_value == "")
                return false;

            value = float.Parse(_value, System.Globalization.CultureInfo.InvariantCulture);
            switch (unit.ToUpper())
            {
                case "EM":
                    lengthType = SVGLengthType.EMs;
                    break;
                case "EX":
                    lengthType = SVGLengthType.EXs;
                    break;
                case "PX":
                    lengthType = SVGLengthType.PX;
                    break;
                case "CM":
                    lengthType = SVGLengthType.CM;
                    break;
                case "MM":
                    lengthType = SVGLengthType.MM;
                    break;
                case "IN":
                    lengthType = SVGLengthType.IN;
                    break;
                case "PT":
                    lengthType = SVGLengthType.PT;
                    break;
                case "PC":
                    lengthType = SVGLengthType.PC;
                    break;
                case "%":
                    lengthType = SVGLengthType.Percentage;
                    break;
                default :
                    lengthType = SVGLengthType.Unknown;
                    break;
            }

            return true;
        }

        public static float ConvertToPX(float value, SVGLengthType lengthType)
        {
            switch (lengthType)
            {
                case SVGLengthType.IN:
                    return value * 90.0f;
                case SVGLengthType.CM:
                    return value * 35.43307f;
                case SVGLengthType.MM:
                    return value * 3.543307f;
                case SVGLengthType.PT:
                    return value * 1.25f;
                case SVGLengthType.PC:
                    return value * 15.0f;
                default:
                    return value;
            }
        }
    }
}
