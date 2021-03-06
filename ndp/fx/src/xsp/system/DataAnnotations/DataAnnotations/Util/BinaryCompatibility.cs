//------------------------------------------------------------------------------
// <copyright file="BinaryCompatibility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.ComponentModel.DataAnnotations.Util {
    using System;
    using System.Runtime.Versioning;
    using System.Configuration;

    internal sealed class BinaryCompatibility {
        // quick accessor for the current AppDomain's instance
        public static readonly BinaryCompatibility Current;

        public static readonly Version Framework40 = new Version(4, 0);
        public static readonly Version Framework472 = new Version(4, 7, 2);
        public static readonly Version FrameworkDefault = Framework40;
        public Version TargetFramework { get; private set; }
        public bool TargetsAtLeastFramework472 { get; private set; }

        static BinaryCompatibility() {
            Current = new BinaryCompatibility();
        }

        public BinaryCompatibility() {
            // Parse version from Target FrameworkName, otherwise use a default value
            Version version = FrameworkDefault;

            if (AppDomain.CurrentDomain.SetupInformation?.TargetFrameworkName != null) {
                // To minimize impact, we don't want the following call to throw exceptions
                // even when the frameworkName string format is incorrect.
                try {
                    FrameworkName frameworkName = new FrameworkName(AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
                    if (frameworkName.Version != null) {
                        version = frameworkName.Version;
                    }
                }
                catch { }
            }
           
            TargetFramework = version;
            TargetsAtLeastFramework472 = (version >= Framework472);
        }
    }
}
