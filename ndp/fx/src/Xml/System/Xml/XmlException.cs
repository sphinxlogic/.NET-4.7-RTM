//------------------------------------------------------------------------------
// <copyright file="XmlException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml {
    using System;
    using System.IO;
    using System.Resources;
    using System.Text;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Globalization;
    using System.Threading;
#if !SILVERLIGHT
    using System.Runtime.Serialization;
#endif

    /// <devdoc>
    ///    <para>Returns detailed information about the last parse error, including the error
    ///       number, line number, character position, and a text description.</para>
    /// </devdoc>
#if !SILVERLIGHT
    [Serializable]
#endif
    public class XmlException : SystemException {
        string res;
        string[] args; // this field is not used, it's here just V1.1 serialization compatibility
        int lineNumber;
        int linePosition; 

#if !SILVERLIGHT
        [OptionalField] 
#endif
        string sourceUri;

        // message != null for V1 exceptions deserialized in Whidbey
        // message == null for V2 or higher exceptions; the exception message is stored on the base class (Exception._message)
        string message;

#if !SILVERLIGHT
        protected XmlException(SerializationInfo info, StreamingContext context) : base(info, context) {
            res                 = (string)  info.GetValue("res"  , typeof(string));
            args                = (string[])info.GetValue("args", typeof(string[]));
            lineNumber          = (int)     info.GetValue("lineNumber", typeof(int));
            linePosition        = (int)     info.GetValue("linePosition", typeof(int));

            // deserialize optional members
            sourceUri = string.Empty;
            string version = null;
            foreach ( SerializationEntry e in info ) {
                switch ( e.Name ) {
                    case "sourceUri":
                        sourceUri = (string)e.Value;
                        break;
                    case "version":
                        version = (string)e.Value;
                        break;
                }
            }

            if ( version == null ) {
                // deserializing V1 exception
                message = CreateMessage( res, args, lineNumber, linePosition );
            }
            else {
                // deserializing V2 or higher exception -> exception message is serialized by the base class (Exception._message)
                message = null;
            }
        }

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("res",                res);
            info.AddValue("args",               args);
            info.AddValue("lineNumber",         lineNumber);
            info.AddValue("linePosition",       linePosition);
            info.AddValue("sourceUri",          sourceUri);
            info.AddValue("version",            "2.0");
        }
#endif

        //provided to meet the ECMA standards
        public XmlException() : this(null) {
        }

        //provided to meet the ECMA standards
        public XmlException(String message) : this (message, ((Exception)null), 0, 0) {
#if DEBUG
            Debug.Assert(message == null || !message.StartsWith("Xml_", StringComparison.Ordinal), "Do not pass a resource here!");
#endif
        }
        
        //provided to meet ECMA standards
        public XmlException(String message, Exception innerException) : this (message, innerException, 0, 0) {
        } 

	//provided to meet ECMA standards
        public XmlException(String message, Exception innerException, int lineNumber, int linePosition) : 
            this( message, innerException, lineNumber, linePosition, null ) {
        }

        internal XmlException(String message, Exception innerException, int lineNumber, int linePosition, string sourceUri) :
            base(FormatUserMessage(message, lineNumber, linePosition), innerException) {

            HResult = HResults.Xml;
            this.res = (message == null ? Res.Xml_DefaultException : Res.Xml_UserException);
            this.args = new string[] { message };
            this.sourceUri = sourceUri;
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        internal XmlException(string res, string[] args) :
            this(res, args, null, 0, 0, null) {}

        internal XmlException(string res, string[] args, string sourceUri) :
            this(res, args, null, 0, 0, sourceUri) {}

        internal XmlException(string res, string arg) :
            this(res, new string[] { arg }, null, 0, 0, null) {}

        internal XmlException(string res, string arg, string sourceUri) :
            this(res, new string[] { arg }, null, 0, 0, sourceUri) {}

        internal XmlException(string res, String arg,  IXmlLineInfo lineInfo) :
            this(res, new string[] { arg }, lineInfo, null) {}

        internal XmlException(string res, String arg, Exception innerException, IXmlLineInfo lineInfo) :
            this(res, new string[] { arg }, innerException, (lineInfo == null ? 0 : lineInfo.LineNumber), (lineInfo == null ? 0 : lineInfo.LinePosition), null) {}

        internal XmlException(string res, String arg,  IXmlLineInfo lineInfo, string sourceUri) :
            this(res, new string[] { arg }, lineInfo, sourceUri) {}

        internal XmlException(string res, string[] args,  IXmlLineInfo lineInfo) :
            this(res, args, lineInfo, null) {}

        internal XmlException(string res, string[] args,  IXmlLineInfo lineInfo, string sourceUri) :
            this (res, args, null, (lineInfo == null ? 0 : lineInfo.LineNumber), (lineInfo == null ? 0 : lineInfo.LinePosition), sourceUri) {
        }

        internal XmlException(string res,  int lineNumber, int linePosition) :
            this(res, (string[])null, null, lineNumber, linePosition) {}

        internal XmlException(string res, string arg, int lineNumber, int linePosition) :
            this(res,  new string[] { arg }, null, lineNumber, linePosition, null) {}

        internal XmlException(string res, string arg, int lineNumber, int linePosition, string sourceUri) :
            this(res,  new string[] { arg }, null, lineNumber, linePosition, sourceUri) {}

        internal XmlException(string res, string[] args, int lineNumber, int linePosition) :
            this( res, args, null, lineNumber, linePosition, null ) {}

        internal XmlException(string res, string[] args, int lineNumber, int linePosition, string sourceUri) :
            this( res, args, null, lineNumber, linePosition, sourceUri ) {}

        internal XmlException(string res, string[] args, Exception innerException, int lineNumber, int linePosition) : 
            this( res, args, innerException, lineNumber, linePosition, null ) {}

        internal XmlException(string res, string[] args, Exception innerException, int lineNumber, int linePosition, string sourceUri) :
            base( CreateMessage(res, args, lineNumber, linePosition), innerException ) {
            HResult = HResults.Xml;
            this.res = res;
            this.args = args;
            this.sourceUri = sourceUri;
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        private static string FormatUserMessage(string message, int lineNumber, int linePosition) {
            if (message == null) {
                return CreateMessage(Res.Xml_DefaultException, null, lineNumber, linePosition);
            }
            else {
                if (lineNumber == 0 && linePosition == 0) {
                    // do not reformat the message when not needed
                    return message;
                }
                else {
                    // add line information
                    return CreateMessage(Res.Xml_UserException, new string[] { message }, lineNumber, linePosition);
                }
            }
        }

        private static string CreateMessage(string res, string[] args, int lineNumber, int linePosition) {
            try {
                string message;

                // No line information -> get resource string and return
                if (lineNumber == 0) {
                    message = Res.GetString(res, args);
                }
                // Line information is available -> we need to append it to the error message
                else {
                    string lineNumberStr = lineNumber.ToString(CultureInfo.InvariantCulture);
                    string linePositionStr = linePosition.ToString(CultureInfo.InvariantCulture);

#if SILVERLIGHT
                    // get the error message from resources
                    bool fallbackUsed;
                    message = Res.GetString(res, out fallbackUsed, args);

                    // If debug resources are available, append the line information
                    if (!fallbackUsed) {
                        message = Res.GetString(Res.Xml_MessageWithErrorPosition, new string[] { message, lineNumberStr, linePositionStr } );
                    }
                    // Debug resources are not available -> add line information to the args and call the GetString to get the default 
                    // fallback message with the updated arguments. We need to handle the the case when the debug resources are not 
                    // available like this; otherwise we would end up with two fallback messages in the final string.
                    else {
                        int origArgCount = args.Length;
                        Array.Resize<string>(ref args, origArgCount + 2);

                        args[origArgCount] = lineNumberStr;
                        args[origArgCount + 1] = linePositionStr;
                        
                        message = Res.GetString(res, args);
                    }
#else
                    message = Res.GetString(res, args);
                    message = Res.GetString(Res.Xml_MessageWithErrorPosition, new string[] { message, lineNumberStr, linePositionStr });
#endif
                }
                return message;
            }
            catch ( MissingManifestResourceException ) {
                return "UNKNOWN("+res+")";
            }
        }

        internal static string[] BuildCharExceptionArgs(string data, int invCharIndex) {
            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < data.Length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char[] data, int invCharIndex) {
            return BuildCharExceptionArgs(data, data.Length, invCharIndex);
        }

        internal static string[] BuildCharExceptionArgs(char[] data, int length, int invCharIndex) {
            Debug.Assert(invCharIndex < data.Length);
            Debug.Assert(invCharIndex < length);
            Debug.Assert(length <= data.Length);

            return BuildCharExceptionArgs(data[invCharIndex], invCharIndex + 1 < length ? data[invCharIndex + 1] : '\0');
        }

        internal static string[] BuildCharExceptionArgs(char invChar, char nextChar) {
            string[] aStringList = new string[2];

            // for surrogate characters include both high and low char in the message so that a full character is displayed
            if (XmlCharType.IsHighSurrogate(invChar) && nextChar != 0) {
                int combinedChar = XmlCharType.CombineSurrogateChar(nextChar, invChar);
                aStringList[0] = new string(new char[] { invChar, nextChar } );
                aStringList[1] = string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", combinedChar);
            }
            else {
                // don't include 0 character in the string - in means eof-of-string in native code, where this may bubble up to
                if ((int)invChar == 0) {
                    aStringList[0] = ".";
                }
                else {
                    aStringList[0] = invChar.ToString(CultureInfo.InvariantCulture);
                }
                aStringList[1] = string.Format(CultureInfo.InvariantCulture, "0x{0:X2}", (int)invChar);
            }
            return aStringList;
        }

        public int LineNumber {
            get { return this.lineNumber; }
        }

        public int LinePosition {
            get { return this.linePosition; }
        }

        public string SourceUri {
            get { return this.sourceUri; }
        }

        public override string Message {
            get { 
                return ( message == null ) ? base.Message : message;
            }
        }

        internal string ResString {
            get {
                return res;
            }
        }

#if !SILVERLIGHT
        internal static bool IsCatchableException(Exception e) {
            Debug.Assert(e != null, "Unexpected null exception");
            return !(
                e is StackOverflowException ||
                e is OutOfMemoryException ||
                e is ThreadAbortException ||
                e is ThreadInterruptedException ||
                e is NullReferenceException ||
                e is AccessViolationException
            );
        }
#endif
    };
} // namespace System.Xml
