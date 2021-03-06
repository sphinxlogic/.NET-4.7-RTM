//------------------------------------------------------------------------------
// <copyright file="OdbcError.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

using System;
using System.Data;

namespace System.Data.Odbc
{
    [Serializable]
    public sealed class OdbcError {
        //Data
        internal string _message;
        internal string _state;
        internal int    _nativeerror;
        internal string _source;

        internal OdbcError(string source, string message, string state, int nativeerror) {
            _source = source;
            _message    = message;
            _state      = state;
            _nativeerror= nativeerror;
        }

        public string Message {
            get {
                return ((null != _message) ? _message : String.Empty);
            }
        }

        public string SQLState {
            get {
                return _state;
            }
        }

        public int NativeError {
            get {
                return _nativeerror;
            }
        }

        public string Source {
            get {
                return ((null != _source) ? _source : String.Empty);
            }
        }

        internal void SetSource (string Source) {
            _source = Source;
        }
        
        override public string ToString() {
            return Message;
        }
    }
}
