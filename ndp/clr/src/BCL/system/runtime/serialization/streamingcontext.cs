// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** ValueType: StreamingContext
**
**
** Purpose: A value type indicating the source or destination of our streaming.
**
**
===========================================================*/
namespace System.Runtime.Serialization {

    using System.Runtime.Remoting;
    using System;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct StreamingContext {
        internal Object m_additionalContext;
        internal StreamingContextStates m_state;
    
        public StreamingContext(StreamingContextStates state) 
            : this (state, null) {
        }
    
        public StreamingContext(StreamingContextStates state, Object additional) {
            m_state = state;
            m_additionalContext = additional;
        }
    
        public Object Context {
            get { return m_additionalContext; }
        }
    
        public override bool Equals(Object obj) {
            if (!(obj is StreamingContext)) {
                return false;
            }
            if (((StreamingContext)obj).m_additionalContext == m_additionalContext &&
                ((StreamingContext)obj).m_state == m_state) {
                return true;
            } 
            return false;
        }
    
        public override int GetHashCode() {
            return (int)m_state;
        }
    
        public StreamingContextStates State {
            get { return m_state; } 
        }
    }
    
    // **********************************************************
    // Keep these in sync with the version in vm\runtimehandles.h
    // **********************************************************
[Serializable]
[Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum StreamingContextStates {
        CrossProcess=0x01,
        CrossMachine=0x02,
        File        =0x04,
        Persistence =0x08,
        Remoting    =0x10,
        Other       =0x20,
        Clone       =0x40,
        CrossAppDomain =0x80,
        All         =0xFF,
    }
}
