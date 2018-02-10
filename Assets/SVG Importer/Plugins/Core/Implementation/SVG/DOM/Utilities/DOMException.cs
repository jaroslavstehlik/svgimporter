// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
