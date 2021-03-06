// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  NeutralResourcesLanguageAttribute
** 
** <OWNER>Microsoft</OWNER>
**
**
** Purpose: Tells the ResourceManager what language your main
**          assembly's resources are written in.  The 
**          ResourceManager won't try loading a satellite
**          assembly for that culture, which helps perf.
**
**
** NOTE:
**
** This custom attribute is no longer implemented in managed code.  As part of a perf optimization,
** it is now read in Module::GetNeutralResourcesLanguage, accessed from ManifestBasedResourceGroveler 
** through an internal runtime call.
===========================================================*/

namespace System.Resources {
    using System;
    using System.Diagnostics.Contracts;
    
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]  
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute 
    {
        private String _culture;
        private UltimateResourceFallbackLocation _fallbackLoc;

        public NeutralResourcesLanguageAttribute(String cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");
            Contract.EndContractBlock();

            _culture = cultureName;
            _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
        }

        public NeutralResourcesLanguageAttribute(String cultureName, UltimateResourceFallbackLocation location)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");
            if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location))
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", location));
            Contract.EndContractBlock();

            _culture = cultureName;
            _fallbackLoc = location;
        }

        public String CultureName {
            get { return _culture; }
        }

        public UltimateResourceFallbackLocation Location {
            get { return _fallbackLoc; }
        }
    }
}
