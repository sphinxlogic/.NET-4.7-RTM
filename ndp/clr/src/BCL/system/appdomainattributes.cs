// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** File: AppDomainAttributes
** 
** <OWNER>Microsoft</OWNER>
**
**
** Purpose: For AppDomain-related custom attributes.
**
**
=============================================================================*/

namespace System {

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum LoaderOptimization 
    {
        NotSpecified            = 0,
        SingleDomain            = 1,
        MultiDomain             = 2,
        MultiDomainHost         = 3,
#if !FEATURE_CORECLR
        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DomainMask              = 3,

        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DisallowBindings        = 4           
#endif        
    }

    [AttributeUsage (AttributeTargets.Method)]  
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class LoaderOptimizationAttribute : Attribute
    {
        internal byte _val;

        public LoaderOptimizationAttribute(byte value)
        {
            _val = value;
        }
        public LoaderOptimizationAttribute(LoaderOptimization value)
        {
            _val = (byte) value;
        }
        public LoaderOptimization Value 
        {  get {return (LoaderOptimization) _val;} }
    }
}

