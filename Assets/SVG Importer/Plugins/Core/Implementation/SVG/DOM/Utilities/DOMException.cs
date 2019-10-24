// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
//using System.Runtime.Serialization;

namespace SVGImporter.Document
{
    public enum DOMExceptionType
    {
        IndexSizeErr,
        DomstringSizeErr,
        HierarchyRequestErr,
        WrongDocumentErr,
        InvalidCharacterErr,
        NoDataAllowedErr,
        NoModificationAllowedErr,
        NotFoundErr,
        NotSupportedErr,
        InuseAttributeErr,
        InvalidStateErr,
        SyntaxErr,
        InvalidModificationErr,
        NamespaceErr,
        InvalidAccessErr
    }

    [Serializable]
    public class DOMException : Exception
    {
        protected DOMException(string msg, Exception innerException) : base(msg, innerException)
        {
        }

        public DOMException(DOMExceptionType code) : this(code, String.Empty)
        {
        }

        public DOMException(DOMExceptionType code, string msg) : this(code, msg, null)
        {
        }

        public DOMException(DOMExceptionType code,
                    string msg,
                    Exception innerException) : base(msg, innerException)
        {
            this.code = code;
        }
        /*
        protected DOMException(SerializationInfo info,
                      StreamingContext context) : base(info, context)
        {
        }
        */
        private DOMExceptionType code;

        public DOMExceptionType Code
        {
            get { return code; }
        }
    }
}
