//------------------------------------------------------------------------------
// <copyright file="XmlSchemaInferenceException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner> 
// <owner current="false" primary="false">Microsoft</owner> 
//------------------------------------------------------------------------------

namespace System.Xml.Schema 
{
    using System;
    using System.IO;
    using System.Resources;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Globalization;

    [Serializable]
    public class XmlSchemaInferenceException : XmlSchemaException 
    {
        

        protected XmlSchemaInferenceException(SerializationInfo info, StreamingContext context) : base(info, context){} 
  

        [SecurityPermissionAttribute(SecurityAction.LinkDemand,SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            base.GetObjectData(info, context);

        }

        public XmlSchemaInferenceException() : base(null) 
        {
        }


        public XmlSchemaInferenceException(String message) : base (message, ((Exception)null), 0, 0) 
        {
        }

        public XmlSchemaInferenceException(String message, Exception innerException) : base (message, innerException, 0, 0) 
        {
        } 

        /// <include file='doc\XmlSchemaException.uex' path='docs/doc[@for="XmlSchemaException.XmlSchemaException3"]/*' />
        public XmlSchemaInferenceException(String message, Exception innerException, int lineNumber, int linePosition) : 
            base(message, innerException, lineNumber, linePosition) 
        {
        }
      
        internal XmlSchemaInferenceException(string res, string[] args) : base(res, args, null, null, 0, 0, null) 
        {
        }
        
        internal XmlSchemaInferenceException(string res, string arg) : base(res, new string[] { arg }, null, null, 0, 0, null) 
        {
        }
        internal XmlSchemaInferenceException(string res, string arg, string sourceUri, int lineNumber, int linePosition) :
            base(res, new string[] { arg }, null, sourceUri, lineNumber, linePosition, null) 
        {
        }

        internal XmlSchemaInferenceException(string res, string sourceUri, int lineNumber, int linePosition) :
            base(res, (string[])null, null, sourceUri, lineNumber, linePosition, null) 
        {
        }

        internal XmlSchemaInferenceException(string res, string[] args, string sourceUri, int lineNumber, int linePosition) :
            base(res, args, null, sourceUri, lineNumber, linePosition, null) 
        {
        }
       
        internal XmlSchemaInferenceException(string res, int lineNumber, int linePosition) :
            base (res, null, null, null, lineNumber, linePosition, null) 
        {

           
        }
        
      
    }
} // namespace System.Xml.Schema


