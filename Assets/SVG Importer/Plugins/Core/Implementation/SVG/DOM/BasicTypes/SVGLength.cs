// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SVGImporter.Utils
{
    public enum SVGLengthType : ushort
    {
        Unknown = 0,
        Number = 1,
        Percentage = 2,
        EMs = 3,
        EXs = 4,
        PX = 5,
        CM = 6,
        MM = 7,
        IN = 8,
        PT = 9,
        PC = 10,
    }

    public struct SVGLength
    {
        private SVGLengthType _unitType;
        private float _valueInSpecifiedUnits, _value;

        public float value
        {
            get { return _value; }
        }

        public SVGLengthType unitType
        {
            get { return _unitType; }
        }

        public SVGLength(SVGLengthType unitType, float valueInSpecifiedUnits)
        {
            _unitType = unitType;
            _valueInSpecifiedUnits = valueInSpecifiedUnits;
            _value = SVGLengthConvertor.ConvertToPX(_valueInSpecifiedUnits, _unitType);
        }

        public SVGLength(float valueInSpecifiedUnits)
        {
            _unitType = SVGLengthType.Number;
            _valueInSpecifiedUnits = valueInSpecifiedUnits;
            _value = SVGLengthConvertor.ConvertToPX(_valueInSpecifiedUnits, _unitType);
        }

        public SVGLength(string valueText)
        {
            float t_value = 0.0f;
            SVGLengthType t_type = SVGLengthType.Unknown;
            SVGLengthConvertor.ExtractType(valueText, ref t_value, ref t_type);
            _unitType = t_type;
            _valueInSpecifiedUnits = t_value;
            _value = SVGLengthConvertor.ConvertToPX(_valueInSpecifiedUnits, _unitType);
        }

        public void NewValueSpecifiedUnits(float valueInSpecifiedUnits)
        {
            _unitType = (SVGLengthType)0;
            _valueInSpecifiedUnits = valueInSpecifiedUnits;
            _value = SVGLengthConvertor.ConvertToPX(_valueInSpecifiedUnits, _unitType);
        }

        public static float GetPXLength(string valueText)
        {
            float t_value = 0.0f;
            SVGLengthType t_type = SVGLengthType.Unknown;
            SVGLengthConvertor.ExtractType(valueText, ref t_value, ref t_type);
            return SVGLengthConvertor.ConvertToPX(t_value, t_type);
        }

        public SVGLength Multiply(SVGLength svglength)
        {
            if(unitType == SVGLengthType.Percentage && svglength.unitType == SVGLengthType.Percentage)
            {
                return new SVGLength(SVGLengthType.Percentage, this.value * svglength.value);                    
            } else {
                return new SVGLength(SVGLengthType.PX, this.value * svglength.value);
            }
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
