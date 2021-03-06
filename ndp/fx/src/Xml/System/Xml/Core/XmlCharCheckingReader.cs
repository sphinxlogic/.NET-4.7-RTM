
//------------------------------------------------------------------------------
// <copyright file="XmlCharCheckingReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml {

    //
    // XmlCharCheckingReaderWithNS
    //
    internal partial class XmlCharCheckingReader : XmlWrappingReader {

//
// Private types
//
        enum State {
            Initial,
            InReadBinary,
            Error,
            Interactive,  // Interactive means other than ReadState.Initial and ReadState.Error; still needs to call
                          // underlying XmlReader to find out if the reported ReadState should be Interactive or EndOfFile
            // NOTE: There is no Closed state here, which is a cause of bugs SQL BU Defect Tracking #533397 and #530403. They were resolved as Won't Fix because they are breaking changes. 
        };

//
// Fields
//
        State state;

        // settings
        bool checkCharacters;
        bool ignoreWhitespace;
        bool ignoreComments;
        bool ignorePis;
        DtdProcessing dtdProcessing; // -1 means do nothing

        XmlNodeType     lastNodeType;
        XmlCharType     xmlCharType;

        ReadContentAsBinaryHelper   readBinaryHelper;

//
// Constructor
//
        internal XmlCharCheckingReader( XmlReader reader, bool checkCharacters, bool ignoreWhitespace, bool ignoreComments, bool ignorePis, DtdProcessing dtdProcessing ) 
            : base( reader ) {

            Debug.Assert( checkCharacters || ignoreWhitespace || ignoreComments || ignorePis || (int)dtdProcessing != -1 );

            state = State.Initial;

            this.checkCharacters = checkCharacters;
            this.ignoreWhitespace = ignoreWhitespace;
            this.ignoreComments = ignoreComments;
            this.ignorePis = ignorePis;
            this.dtdProcessing = dtdProcessing;

            lastNodeType = XmlNodeType.None;

            if ( checkCharacters ) {
                xmlCharType = XmlCharType.Instance;
            }
        }

//
// XmlReader implementation
//
        public override XmlReaderSettings Settings {
            get {
                XmlReaderSettings settings = reader.Settings;
                if ( settings == null ) {
                    settings = new XmlReaderSettings();
                }
                else {
                    settings = settings.Clone();
                }

                if ( checkCharacters ) {
                    settings.CheckCharacters = true;
                }
                if ( ignoreWhitespace ) {
                    settings.IgnoreWhitespace = true;
                }
                if ( ignoreComments ) {
                    settings.IgnoreComments = true;
                }
                if ( ignorePis ) {
                    settings.IgnoreProcessingInstructions = true;
                }
                if ( (int)dtdProcessing != -1 ) {
                    settings.DtdProcessing = dtdProcessing;
                }
                settings.ReadOnly = true;
                return settings;
            }
        }

        public override bool MoveToAttribute( string name ) {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.MoveToAttribute( name );
        }

        public override bool MoveToAttribute( string name, string ns ) {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.MoveToAttribute( name, ns );
        }

        public override void MoveToAttribute( int i ) {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            base.reader.MoveToAttribute( i );
        }

        public override bool MoveToFirstAttribute() {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute() {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.MoveToNextAttribute();
        }

        public override bool MoveToElement() {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.MoveToElement();
        }

        public override  bool  Read() {
            switch ( state ) {
                case State.Initial:
                    state = State.Interactive;
                    if ( base.reader.ReadState == ReadState.Initial ) {
                        goto case State.Interactive;
                    }
                    break;

                case State.Error:
                    return false;

                case State.InReadBinary:
                    FinishReadBinary();
                    state = State.Interactive;
                    goto case State.Interactive;

                case State.Interactive:
                    if ( !base.reader.Read() ) {
                        return false;
                    }
                    break;

                default:
                    Debug.Assert( false );
                    return false;
            }

            XmlNodeType nodeType = base.reader.NodeType;

            if ( !checkCharacters ) {
                switch ( nodeType ) {
                    case XmlNodeType.Comment:
                        if ( ignoreComments ) {
                            return Read();
                        }
                        break;
                    case XmlNodeType.Whitespace:
                        if ( ignoreWhitespace ) {
                            return Read();
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        if ( ignorePis ) {
                            return Read();
                        }
                        break;
                    case XmlNodeType.DocumentType:
                        if ( dtdProcessing == DtdProcessing.Prohibit ) {
                            Throw( Res.Xml_DtdIsProhibitedEx, string.Empty );
                        }
                        else if ( dtdProcessing == DtdProcessing.Ignore ) {
                            return Read();
                        }
                        break;
                }
                return true;
            }
            else {
                switch ( nodeType ) {
                    case XmlNodeType.Element:
                        if ( checkCharacters ) {
                            // check element name
                            ValidateQName( base.reader.Prefix, base.reader.LocalName );

                            // check values of attributes
                            if ( base.reader.MoveToFirstAttribute() ) {
                                do {
                                    ValidateQName( base.reader.Prefix, base.reader.LocalName );
                                    CheckCharacters( base.reader.Value );
                                } while ( base.reader.MoveToNextAttribute() );

                                base.reader.MoveToElement();
                            }
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        if ( checkCharacters ) {
                            CheckCharacters( base.reader.Value );
                        }
                        break;

                    case XmlNodeType.EntityReference:
                        if ( checkCharacters ) {
                            // check name
                            ValidateQName( base.reader.Name );
                        }
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        if ( ignorePis ) {
                            return Read();
                        }
                        if ( checkCharacters ) {
                            ValidateQName( base.reader.Name );
                            CheckCharacters( base.reader.Value );
                        }
                        break;

                    case XmlNodeType.Comment:
                        if ( ignoreComments ) {
                            return Read();
                        }
                        if ( checkCharacters ) {
                            CheckCharacters( base.reader.Value );
                        }
                        break;

                    case XmlNodeType.DocumentType:
                        if ( dtdProcessing == DtdProcessing.Prohibit ) {
                            Throw( Res.Xml_DtdIsProhibitedEx, string.Empty );
                        }
                        else if ( dtdProcessing == DtdProcessing.Ignore ) {
                            return Read();
                        }
                        if ( checkCharacters ) {
                            ValidateQName( base.reader.Name );
                            CheckCharacters( base.reader.Value );
                            
                            string str;
                            str = base.reader.GetAttribute( "SYSTEM" );
                            if ( str != null ) {
                                CheckCharacters( str );
                            }

                            str = base.reader.GetAttribute( "PUBLIC" );
                            if ( str != null ) {
                                int i;
                                if ( ( i = xmlCharType.IsPublicId( str ) ) >= 0 ) {
                                    Throw( Res.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs( str, i ) );
                                }
                            }
                        }
                        break;

                    case XmlNodeType.Whitespace:
                        if ( ignoreWhitespace ) {
                            return Read();
                        }
                        if ( checkCharacters ) {
                            CheckWhitespace( base.reader.Value );
                        }
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        if ( checkCharacters ) {
                            CheckWhitespace( base.reader.Value );
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if ( checkCharacters ) {
                            ValidateQName( base.reader.Prefix, base.reader.LocalName );
                        }
                        break;

                    default:
                        break;
                }
                lastNodeType = nodeType;
                return true;
            }
        }

        public override ReadState ReadState { 
            get {
                switch ( state ) {
                    case State.Initial:
                        return base.reader.ReadState == ReadState.Closed ? ReadState.Closed : ReadState.Initial;
                    case State.Error:
                        return ReadState.Error;
                    case State.InReadBinary:
                    case State.Interactive: 
                    default:
                        return base.reader.ReadState;
                }
            }
        }

        public override bool ReadAttributeValue() {
            if ( state == State.InReadBinary ) {
                FinishReadBinary();
            }
            return base.reader.ReadAttributeValue();
        }

        public override bool CanReadBinaryContent {
            get {  
                return true;
            }
        }

        public override  int  ReadContentAsBase64( byte[] buffer, int index, int count ) {
            if (ReadState != ReadState.Interactive) {
                return 0;
            }

            if ( state != State.InReadBinary ) {
                // forward ReadBase64Chunk calls into the base (wrapped) reader if possible, i.e. if it can read binary and we 
                // should not check characters
                if ( base.CanReadBinaryContent && ( !checkCharacters ) ) {
                    readBinaryHelper = null;
                    state = State.InReadBinary;
                    return base.ReadContentAsBase64( buffer, index, count );
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore white spaces
                else {
                    readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset( readBinaryHelper, this );
                }
            }
            else { 
                // forward calls into wrapped reader 
                if ( readBinaryHelper == null ) {
                    return base.ReadContentAsBase64( buffer, index, count );
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = State.Interactive;

            // call to the helper
            int readCount = readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // turn on InReadBinary in again and return
            state = State.InReadBinary;
            return readCount;
        }

        public override  int  ReadContentAsBinHex( byte[] buffer, int index, int count ) {
            if (ReadState != ReadState.Interactive) {
                return 0;
            }

            if ( state != State.InReadBinary ) {
                // forward ReadBinHexChunk calls into the base (wrapped) reader if possible, i.e. if it can read chunks and we 
                // should not check characters
                if ( base.CanReadBinaryContent && ( !checkCharacters ) ) {
                    readBinaryHelper = null;
                    state = State.InReadBinary;
                    return base.ReadContentAsBinHex( buffer, index, count );
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore white spaces
                else {
                    readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset( readBinaryHelper, this );
                }
            }
            else { 
                // forward calls into wrapped reader 
                if ( readBinaryHelper == null ) {
                    return base.ReadContentAsBinHex( buffer, index, count );
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = State.Interactive;

            // call to the helper
            int readCount = readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // turn on InReadBinary in again and return
            state = State.InReadBinary;
            return readCount;        
        }

        public override  int  ReadElementContentAsBase64( byte[] buffer, int index, int count ) {
            // check arguments
            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }
            if (buffer.Length - index < count) {
                throw new ArgumentOutOfRangeException("count");
            }

            if (ReadState != ReadState.Interactive) {
                return 0;
            }

            if ( state != State.InReadBinary ) {
                // forward ReadBase64Chunk calls into the base (wrapped) reader if possible, i.e. if it can read binary and we 
                // should not check characters
                if ( base.CanReadBinaryContent && ( !checkCharacters ) ) {
                    readBinaryHelper = null;
                    state = State.InReadBinary;
                    return base.ReadElementContentAsBase64( buffer, index, count );
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore white spaces
                else {
                    readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset( readBinaryHelper, this );
                }
            }
            else { 
                // forward calls into wrapped reader 
                if ( readBinaryHelper == null ) {
                    return base.ReadElementContentAsBase64( buffer, index, count );
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = State.Interactive;

            // call to the helper
            int readCount = readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // turn on InReadBinary in again and return
            state = State.InReadBinary;
            return readCount;
        }

        public override  int  ReadElementContentAsBinHex( byte[] buffer, int index, int count ) {
            // check arguments
            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }
            if (buffer.Length - index < count) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (ReadState != ReadState.Interactive) {
                return 0;
            }

            if ( state != State.InReadBinary ) {
                // forward ReadBinHexChunk calls into the base (wrapped) reader if possible, i.e. if it can read chunks and we 
                // should not check characters
                if ( base.CanReadBinaryContent && ( !checkCharacters ) ) {
                    readBinaryHelper = null;
                    state = State.InReadBinary;
                    return base.ReadElementContentAsBinHex( buffer, index, count );
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore white spaces
                else {
                    readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset( readBinaryHelper, this );
                }
            }
            else { 
                // forward calls into wrapped reader 
                if ( readBinaryHelper == null ) {
                    return base.ReadElementContentAsBinHex( buffer, index, count );
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = State.Interactive;

            // call to the helper
            int readCount = readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // turn on InReadBinary in again and return
            state = State.InReadBinary;
            return readCount;        
        }

//
// Private methods and properties
//

        private void Throw( string res, string arg ) {
            state = State.Error;
            throw new XmlException( res, arg, (IXmlLineInfo)null );
        }

        private void Throw( string res, string[] args ) {
            state = State.Error;
            throw new XmlException( res, args, (IXmlLineInfo)null );
        }

        private void CheckWhitespace( string value ) {
            int i;
            if ( ( i = xmlCharType.IsOnlyWhitespaceWithPos( value ) ) != -1 ) {
                Throw( Res.Xml_InvalidWhitespaceCharacter, XmlException.BuildCharExceptionArgs( value, i ) );
            }
        }

        private void ValidateQName( string name ) {
            string prefix, localName;
            ValidateNames.ParseQNameThrow( name, out prefix, out localName );
        }

        private void ValidateQName( string prefix, string localName ) {
            try {
                if ( prefix.Length > 0 ) {
                    ValidateNames.ParseNCNameThrow( prefix );
                }
                ValidateNames.ParseNCNameThrow( localName );
            }
            catch {
                state = State.Error;
                throw;
            }
        }

        private void CheckCharacters( string value ) {
            XmlConvert.VerifyCharData( value, ExceptionType.ArgumentException, ExceptionType.XmlException );
        }

        private void FinishReadBinary() {
            state = State.Interactive;
            if ( readBinaryHelper != null ) {
                readBinaryHelper.Finish();
            }
        }

    }

    //
    // XmlCharCheckingReaderWithNS
    //
    internal class XmlCharCheckingReaderWithNS : XmlCharCheckingReader, IXmlNamespaceResolver {

        internal IXmlNamespaceResolver readerAsNSResolver;

        internal XmlCharCheckingReaderWithNS( XmlReader reader, IXmlNamespaceResolver readerAsNSResolver, bool checkCharacters, bool ignoreWhitespace, bool ignoreComments, bool ignorePis, DtdProcessing dtdProcessing ) 
            : base( reader, checkCharacters, ignoreWhitespace, ignoreComments, ignorePis, dtdProcessing ) {
            Debug.Assert( readerAsNSResolver != null );
            this.readerAsNSResolver = readerAsNSResolver;
        }
//
// IXmlNamespaceResolver
//
        IDictionary<string,string> IXmlNamespaceResolver.GetNamespacesInScope( XmlNamespaceScope scope ) {
            return readerAsNSResolver.GetNamespacesInScope( scope );
        }

        string IXmlNamespaceResolver.LookupNamespace( string prefix ) {
            return readerAsNSResolver.LookupNamespace( prefix );
        }

        string IXmlNamespaceResolver.LookupPrefix( string namespaceName ) {
            return readerAsNSResolver.LookupPrefix( namespaceName );
        }

    }
}
