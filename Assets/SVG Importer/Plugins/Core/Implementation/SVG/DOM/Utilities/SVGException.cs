// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
//using System.Runtime.Serialization;

namespace SVGImporter.Document
{
    public enum SVGExceptionType
    {
        WrongType,
        InvalidValue,
        MatrixNotInvertable
    }

    public class SVGException : DOMException
    {
        public SVGException(SVGExceptionType errorCode) :this(errorCode, String.Empty, null)
        {

        }

        public SVGException(SVGExceptionType errorCode, string message) :this(errorCode, message, null)
        {
        }

        public SVGException(SVGExceptionType errorCode, string message, Exception innerException) :base(message, innerException)
        {
            code = errorCode;
        }
        /*
        protected SVGException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        */
        private SVGExceptionType code;

        public new SVGExceptionType Code
        {
            get
            {
                return code;
            }
        }
    }
}
