// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  StringToken
** 
** <OWNER>Microsoft</OWNER>
**
**
** Purpose: Represents a String to the ILGenerator class.
**
** 
===========================================================*/
namespace System.Reflection.Emit {
    
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct StringToken {
    
        internal int m_string;
    
        //public StringToken() {
        //    m_string=0;
        //}
        
        internal StringToken(int str) {
            m_string=str;
        }
    
        // Returns the metadata token for this particular string.  
        // Generated by a call to Module.GetStringConstant().
        //
        public int Token {
            get { return m_string; }
        }
        
        public override int GetHashCode()
        {
            return m_string;
        }
        
        public override bool Equals(Object obj)
        {
            if (obj is StringToken)
                return Equals((StringToken)obj);
            else
                return false;
        }
    
        public bool Equals(StringToken obj)
        {
            return obj.m_string == m_string;
        }
    
        public static bool operator ==(StringToken a, StringToken b)
        {
            return a.Equals(b);
        }
        
        public static bool operator !=(StringToken a, StringToken b)
        {
            return !(a == b);
        }
        
    }








}
