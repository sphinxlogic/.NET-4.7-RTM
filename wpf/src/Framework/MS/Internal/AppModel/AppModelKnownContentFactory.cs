//---------------------------------------------------------------------------
//
// <copyright file="AppModelKnownContentFactory.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// Description:
// Provides a method to turn a baml stream into an object.
//
//
// History:
//  11/22/04:    Microsoft:       Initial Creation
//
//
//---------------------------------------------------------------------------
using System;
using System.IO;

using System.Net; // WebPermission. 
using System.Security; 
using System.Security.Permissions; 
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using MS.Internal.Controls;
using MS.Internal.Navigation;
using MS.Internal.Utility;
using MS.Internal.Resources;
using System.IO.Packaging;
using MS.Internal.PresentationFramework;
using System.ComponentModel;
using System.Windows.Controls;

namespace MS.Internal.AppModel
{
    // !!!! Note: Those methods are registered as MimeObjectFactory.StreamToObjectFactoryDelegate. The caller expects the 
    // delgate to close stream. 
    internal static class AppModelKnownContentFactory
    {
        // <summary>
        // Creates an object instance from a Baml stream and it's Uri
        // </summary>
        internal static object BamlConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            asyncObjectConverter = null;

            // If this stream comes from outside the application throw
            //
            if (!BaseUriHelper.IsPackApplicationUri(baseUri))
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlIsNotSupportedOutsideOfApplicationResources));
            }

            // If this stream comes from a content file also throw
            Uri partUri = PackUriHelper.GetPartUri(baseUri);
            string partName, assemblyName, assemblyVersion, assemblyKey;
            BaseUriHelper.GetAssemblyNameAndPart(partUri, out partName, out assemblyName, out assemblyVersion, out assemblyKey);
            if (ContentFileHelper.IsContentFile(partName))
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlIsNotSupportedOutsideOfApplicationResources));
            }

            ParserContext pc = new ParserContext();

            pc.BaseUri = baseUri;
            pc.SkipJournaledProperties = isJournalNavigation;

            return Application.LoadBamlStreamWithSyncInfo(stream, pc);
        }

        // <summary>
        // Creates an object instance from a Xaml stream and it's Uri
        // </summary>
        ///<SecurityNote>
        /// Critical    - Keep track of this, as sandboxing is meant to be a way for an application to 
        ///                 isolate xaml content from the hosting application.
        /// TreatAsSafe - the act of putting this content in a new webbrowser control (and by extention, a new PresentationHost) 
        ///               isolates the xaml content from the rest of the application.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static object XamlConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            asyncObjectConverter = null;

            if (sandboxExternalContent)
            {
                if (SecurityHelper.AreStringTypesEqual(baseUri.Scheme, BaseUriHelper.PackAppBaseUri.Scheme))
                {
                    baseUri = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(baseUri);
                }

                stream.Close();

                WebBrowser webBrowser = new WebBrowser();
                webBrowser.Source = baseUri;
                return webBrowser;
            }
            else
            {
                ParserContext pc = new ParserContext();

                pc.BaseUri = baseUri;
                pc.SkipJournaledProperties = isJournalNavigation;

                if (allowAsync)
                {
                    XamlReader xr = new XamlReader();
                    asyncObjectConverter = xr;
                    xr.LoadCompleted += new AsyncCompletedEventHandler(OnParserComplete);
                    // XamlReader.Load will close the stream.
                    return xr.LoadAsync(stream, pc);
                }
                else
                {
                    // XamlReader.Load will close the stream.
                    return XamlReader.Load(stream, pc);
                }
            }
        }

	    private static void OnParserComplete(object sender, AsyncCompletedEventArgs args)
        {
            // We can get this event from cancellation. We do not care about the error if there is any
            // that happened as a result of cancellation.
            if ((! args.Cancelled) && (args.Error != null))
            {
                throw args.Error;
            }
        }

        internal static object HtmlXappConverter(Stream stream, Uri baseUri, bool canUseTopLevelBrowser, bool sandboxExternalContent, bool allowAsync, bool isJournalNavigation, out XamlReader asyncObjectConverter)
        {
            asyncObjectConverter = null;

            if (canUseTopLevelBrowser)
            {
                return null;
            }
            
            if (SecurityHelper.AreStringTypesEqual(baseUri.Scheme, BaseUriHelper.PackAppBaseUri.Scheme))
            {
                baseUri = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(baseUri);
            }

            stream.Close();

            WebBrowser webBrowser = new WebBrowser();
            webBrowser.Source = baseUri;

            return webBrowser;
        }
    }
}
