//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// File: AppContextDefaultValues.cs
//---------------------------------------------------------------------------

using MS.Internal;

namespace System
{
    // There are cases where we have multiple assemblies that are going to import this file and
    // if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
    // that the type is found both in the source and in a referenced assembly. The compiler will prefer
    // the version of the type defined in the source
    //
    // In order to disable the warning for this type we are disabling this warning for this entire file.
    #pragma warning disable 436

    internal static partial class AppContextDefaultValues
    {
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int targetFrameworkVersion)
        {
            switch (platformIdentifier)
            {
                case ".NETFramework":
                    {
                        if (targetFrameworkVersion <= 40502)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40602)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpaceSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40700)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40701)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, true);
                        }

                        // DDVSO:405199
                        // The standard behavior is to draw Text/PasswordBox selections via the adorner.
                        // We want this to always be the case unless it is explicity changed, regardless of .NET target version.
                        LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRenderingSwitchName, true);

                        break;
                    }
            }
        }
    }

    #pragma warning restore 436
}
