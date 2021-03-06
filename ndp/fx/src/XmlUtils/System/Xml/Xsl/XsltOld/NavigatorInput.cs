//------------------------------------------------------------------------------
// <copyright file="NavigatorInput.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">Microsoft</owner>
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.XsltOld {
    using Res = System.Xml.Utils.Res;
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using Keywords = System.Xml.Xsl.Xslt.KeywordsTable;

    internal class NavigatorInput {
        private XPathNavigator     _Navigator;
        private PositionInfo       _PositionInfo;
        private InputScopeManager  _Manager;
        private NavigatorInput     _Next;
        private string             _Href;
        private Keywords           _Atoms;

        internal NavigatorInput Next {
            get {
                return _Next;
            }
            set {
                _Next = value;
            }
        }

        internal string Href {
            get {
                return _Href;
            }
        }

        internal Keywords Atoms {
            get {
                return _Atoms;
            }
        }        

        internal  XPathNavigator Navigator {
            get {
                AssertInput();
                return _Navigator;
            }
        }

        internal  InputScopeManager InputScopeManager {
            get {
                AssertInput();
                return _Manager;
            }
        }

        internal  bool Advance() {
            AssertInput();
            return _Navigator.MoveToNext();
        }

        internal  bool Recurse() {
            AssertInput();
            return _Navigator.MoveToFirstChild();
        }

        internal  bool ToParent() {
            AssertInput();
            return _Navigator.MoveToParent();
        }

        internal  void Close() {
            _Navigator = null;
            _PositionInfo = null;
        }

        //
        // Input document properties
        //

        //
        // XPathNavigator does not support line and position numbers
        //

        internal  int LineNumber { 
            get { return _PositionInfo.LineNumber; }
        }

        internal  int LinePosition { 
            get { return _PositionInfo.LinePosition; }
        }

        internal  XPathNodeType NodeType {
            get {
                AssertInput();
                return _Navigator.NodeType;
            }
        }

        internal  string Name {
            get {
                AssertInput();
                return _Navigator.Name;
            }
        }

        internal  string LocalName {
            get {
                AssertInput();
                return _Navigator.LocalName;
            }
        }

        internal  string NamespaceURI {
            get {
                AssertInput();
                return _Navigator.NamespaceURI;
            }
        }

        internal  string Prefix {
            get {
                AssertInput();
                return _Navigator.Prefix;
            }
        }

        internal  string Value {
            get {
                AssertInput();
                return _Navigator.Value;
            }
        }

        internal  bool IsEmptyTag {
            get {
                AssertInput();
                return _Navigator.IsEmptyElement;
            }
        }

        internal  string BaseURI {
            get {
                return  _Navigator.BaseURI;
            }
        }
        
        internal  bool MoveToFirstAttribute() {
            AssertInput();
            return _Navigator.MoveToFirstAttribute();
        }

        internal  bool MoveToNextAttribute() {
            AssertInput();
            return _Navigator.MoveToNextAttribute();
        }
        internal  bool MoveToFirstNamespace() {
            AssertInput();
            return _Navigator.MoveToFirstNamespace(XPathNamespaceScope.ExcludeXml);
        }

        internal  bool MoveToNextNamespace() {
            AssertInput();
            return _Navigator.MoveToNextNamespace(XPathNamespaceScope.ExcludeXml);
        }

        //
        // Constructor
        //
        internal NavigatorInput(XPathNavigator navigator, string baseUri, InputScope rootScope) {
            if (navigator == null) {
                throw new ArgumentNullException("navigator");
            }
            if (baseUri == null) {
                throw new ArgumentNullException("baseUri");
            }
            Debug.Assert(navigator.NameTable != null);
            _Next  = null;
            _Href  = baseUri;
            _Atoms = new Keywords(navigator.NameTable);
            _Navigator = navigator;
            _Manager   = new InputScopeManager(_Navigator, rootScope);
            _PositionInfo = PositionInfo.GetPositionInfo(_Navigator);

            /*BeginReading:*/
            AssertInput();
            if (NodeType == XPathNodeType.Root) {
                _Navigator.MoveToFirstChild();
            }
        }

        internal NavigatorInput(XPathNavigator navigator): this(navigator, navigator.BaseURI, null) {}

        //
        // Debugging support
        //
        [System.Diagnostics.Conditional("DEBUG")]
        internal void AssertInput() {
            Debug.Assert(_Navigator != null);
        }
    }
}
