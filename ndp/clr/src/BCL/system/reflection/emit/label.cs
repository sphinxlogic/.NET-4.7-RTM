// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  Label
** 
** <OWNER>Microsoft</OWNER>
**
**  
**
** Purpose: Represents a Label to the ILGenerator class.
**
** 
===========================================================*/
namespace System.Reflection.Emit {
    using System;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    // The Label class is an opaque representation of a label used by the 
    // ILGenerator class.  The token is used to mark where labels occur in the IL
    // stream and then the necessary offsets are put back in the code when the ILGenerator 
    // is passed to the MethodWriter.
    // Labels are created by using ILGenerator.CreateLabel and their position is set
    // by using ILGenerator.MarkLabel.
    [Serializable]
    [ComVisible(true)]
    public struct Label {
    
        internal int m_label;
    
        //public Label() {
        //    m_label=0;
        //}
        
        internal Label (int label) {
            m_label=label;
        }
    
        internal int GetLabelValue() {
            return m_label;
        }
        
        public override int GetHashCode()
        {
            return m_label;
        }
        
        public override bool Equals(Object obj)
        {
            if (obj is Label)
                return Equals((Label)obj);
            else
                return false;
        }
        
        public bool Equals(Label obj)
        {
            return obj.m_label == m_label;
        }
    
        public static bool operator ==(Label a, Label b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(Label a, Label b)
        {
            return !(a == b);
        }
    }
}
