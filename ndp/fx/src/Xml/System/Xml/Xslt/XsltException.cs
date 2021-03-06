//------------------------------------------------------------------------------
// <copyright file="XsltException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml.XPath;

namespace System.Xml.Xsl {
    using Res = System.Xml.Utils.Res;

    [Serializable]
    public class XsltException : SystemException {
        string      res;
        string[]    args;
        string      sourceUri;
        int         lineNumber;
        int         linePosition;

        // message != null for V1 & V2 exceptions deserialized in Whidbey
        // message == null for created V2 exceptions; the exception message is stored in Exception._message
        string      message;

        protected XsltException(SerializationInfo info, StreamingContext context) : base(info, context) {
            res          = (string)   info.GetValue("res"         , typeof(string           ));
            args         = (string[]) info.GetValue("args"        , typeof(string[]         ));
            sourceUri    = (string)   info.GetValue("sourceUri"   , typeof(string           ));
            lineNumber   = (int)      info.GetValue("lineNumber"  , typeof(int              ));
            linePosition = (int)      info.GetValue("linePosition", typeof(int              ));

            // deserialize optional members
            string version = null;
            foreach ( SerializationEntry e in info ) {
                if ( e.Name == "version" ) {
                    version = (string)e.Value;
                }
            }

            if (version == null) {
                // deserializing V1 exception
                message = CreateMessage(res, args, sourceUri, lineNumber, linePosition);
            }
            else {
                // deserializing V2 or higher exception -> exception message is serialized by the base class (Exception._message)
                message = null;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("res"         , res         );
            info.AddValue("args"        , args        );
            info.AddValue("sourceUri"   , sourceUri   );
            info.AddValue("lineNumber"  , lineNumber  );
            info.AddValue("linePosition", linePosition);
            info.AddValue("version"     , "2.0");
        }

        public XsltException() : this (string.Empty, (Exception) null) {}

        public XsltException(String message) : this (message, (Exception) null) {}

        public XsltException(String message, Exception innerException) :
            this(Res.Xml_UserException, new string[] { message }, null, 0, 0, innerException ) {
        }

        internal static XsltException Create(string res, params string[] args) {
            return new XsltException(res, args, null, 0, 0, null);
        }

        internal static XsltException Create(string res, string[] args, Exception inner) {
            return new XsltException(res, args, null, 0, 0, inner);
        }

        internal XsltException(string res, string[] args, string sourceUri, int lineNumber, int linePosition, Exception inner)
            : base(CreateMessage(res, args, sourceUri, lineNumber, linePosition), inner)
        {
            HResult           = HResults.XmlXslt;
            this.res          = res;
            this.sourceUri    = sourceUri;
            this.lineNumber   = lineNumber;
            this.linePosition = linePosition;
        }

        public virtual string SourceUri {
            get { return this.sourceUri; }
        }

        public virtual int LineNumber {
            get { return this.lineNumber; }
        }

        public virtual int LinePosition {
            get { return this.linePosition; }
        }

        public override string Message {
            get {
                return (message == null) ? base.Message : message;
            }
        }

        private static string CreateMessage(string res, string[] args, string sourceUri, int lineNumber, int linePosition) {
            try {
                string message = FormatMessage(res, args);
                if (res != Res.Xslt_CompileError && lineNumber != 0) {
                    message += " " + FormatMessage(Res.Xml_ErrorFilePosition, sourceUri, lineNumber.ToString(CultureInfo.InvariantCulture), linePosition.ToString(CultureInfo.InvariantCulture));
                }
                return message;
            }
            catch (MissingManifestResourceException) {
                return "UNKNOWN(" + res + ")";
            }
        }

        private static string FormatMessage(string key, params string[] args) {
            string message = Res.GetString(key);
            if (message != null && args != null) {
                message = string.Format(CultureInfo.InvariantCulture, message, args);
            }
            return message;
        }
    }

    [Serializable]
    public class XsltCompileException : XsltException {

        protected XsltCompileException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
        }

        public XsltCompileException() : base() {}

        public XsltCompileException(String message) : base (message) {}

        public XsltCompileException(String message, Exception innerException) : base (message, innerException) {}

        public XsltCompileException(Exception inner, string sourceUri, int lineNumber, int linePosition) :
            base(
                lineNumber != 0 ? Res.Xslt_CompileError : Res.Xslt_CompileError2,
                new string[] { sourceUri, lineNumber.ToString(CultureInfo.InvariantCulture), linePosition.ToString(CultureInfo.InvariantCulture) },
                sourceUri, lineNumber, linePosition, inner
            ) {}
    }
}
