//---------------------------------------------------------------------------
//
// File: Extensions.cs
//
// Description: Extension methods for use with MsSpellCheckLib.RCW interfaces
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Windows.Documents.MsSpellCheckLib
{
    using IEnumString = RCW.IEnumString;
    using IEnumSpellingError = RCW.IEnumSpellingError;
    using ISpellingError = RCW.ISpellingError;

    using SpellingError = SpellChecker.SpellingError;
    using CorrectiveAction = SpellChecker.CorrectiveAction;

    /// <summary>
    /// Extension methods for use with MsSpellCheckLib.RCW interfaces
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Extracts a list of strings from an RCW.IEnumString instance.
        /// </summary>
        /// <SecurityNote>
        ///     Critical - calls into COM
        ///     Safe - Does not expose any unmanaged resources to the caller
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static List<string> ToList(
            this IEnumString enumString, 
            bool shouldSuppressCOMExceptions = true, 
            bool shouldReleaseCOMObject = true)
        {
            var result = new List<string>();

            if (enumString == null)
            {
                throw new ArgumentNullException(nameof(enumString));
            }

            try
            {
                uint fetched = 0;
                string str = string.Empty;

                do
                {
                    enumString.RemoteNext(1, out str, out fetched);
                    if (fetched > 0)
                    {
                        result.Add(str);
                    }
                }
                while (fetched > 0);
            }
            catch (COMException) when (shouldSuppressCOMExceptions)
            {
                // do nothing here
                // the exception filter does it all
            }
            finally
            {
                if (shouldReleaseCOMObject)
                {
                    Marshal.ReleaseComObject(enumString);
                }
            }

            return result;
        }


        /// <summary>
        /// Extracts a list of SpellingError's from an RCW.IEnumSpellingError instance.
        /// </summary>
        /// <SecurityNote>
        ///     Critical - Calls into COM 
        ///     Safe - Does not expose any unmanaged resources to the caller
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static List<SpellingError> ToList(
            this IEnumSpellingError spellingErrors, 
            SpellChecker spellChecker, 
            string text, 
            bool shouldSuppressCOMExceptions = true, 
            bool shouldReleaseCOMObject = true)
        {
            if (spellingErrors == null)
            {
                throw new ArgumentNullException(nameof(spellingErrors));
            }

            var result = new List<SpellingError>();

            try
            {
                while (true)
                {
                    ISpellingError iSpellingError = spellingErrors.Next();

                    if (iSpellingError == null)
                    {
                        // no more ISpellingError objects left in the enum
                        break; 
                    }

                    var error = new SpellingError(iSpellingError, spellChecker, text, shouldSuppressCOMExceptions, true);
                    result.Add(error);
                }
            }
            catch (COMException) when (shouldSuppressCOMExceptions)
            {
                // do nothing here 
                // the exception filter does it all. 
            }
            finally
            {
                if (shouldReleaseCOMObject)
                {
                    Marshal.ReleaseComObject(spellingErrors);
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether a collection of SpellingError instances
        /// has any actual errors, or whether they represent a 'clean'
        /// result. 
        /// </summary>
        internal static bool IsClean(this List<SpellingError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            bool isClean = true; 
            foreach (var error in errors)
            {
                if (error.CorrectiveAction != CorrectiveAction.None)
                {
                    isClean = false;
                    break;
                }
            }

            return isClean;
        }
    }
}
