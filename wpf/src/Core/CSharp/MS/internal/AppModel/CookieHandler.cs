//+-----------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  Description:
//      A helper class for access to HTTP cookies and attaching cookies to HttpWebRequests and storing
//      cookies from HttpWebResponses. 
//
//      In standalone WPF applications, the WinInet cookie store is used. PresentationHost intercepts calls
//      to the WinInet cookie functions and delegates them to the browser. See host\DLL\CookieShim.hxx.
//
//  History:
//     2007/04/11   Microsoft     Created
//
//------------------------------------------------------------------------

using System;
using System.Net;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

using System.Windows;
using System.Windows.Interop;
using MS.Win32;
using MS.Internal.PresentationCore;

namespace MS.Internal.AppModel
{

static class CookieHandler
{
    internal static void HandleWebRequest(WebRequest request)
    {
        HttpWebRequest httpRequest = request as HttpWebRequest;
        if (httpRequest != null)
        {
            try
            {
                string cookies = GetCookie(httpRequest.RequestUri, false/*throwIfNoCookie*/);
                if(!string.IsNullOrEmpty(cookies))
                {
                    if (httpRequest.CookieContainer == null)
                    {
                        httpRequest.CookieContainer = new CookieContainer();
                    }
                    // CookieContainer.SetCookies() expects multiple cookie definitions to be separated by 
                    // comma, but GetCookie() returns them separated by ';', so we change that.
                    // Comma is generally not valid within a cookie (except in the 'expires' date setting, but 
                    // we don't get that from GetCookie()). 
                    // ClickOnce does the same in System.Deployment.Application.SystemNetDownloader.DownloadSingleFile().
                    httpRequest.CookieContainer.SetCookies(httpRequest.RequestUri, cookies.Replace(';', ','));
                }
            }
            catch (Exception ex) // Attaching cookies shouldn't fail a web request.
            {
                if (CriticalExceptions.IsCriticalException(ex))
                    throw;
            }
        }
    }

    /// <summary>
    /// Extracts cookies from a (Http)WebResponse and stores them.
    /// </summary>
    /// <SecurityNote>
    /// Critical: Calls SetCookieUnsafe(). 
    ///     An authentic WebResponse is expected, not altered by untrusted code. P3P headers fabricated by the 
    ///     application cannot be trusted. And the application should have been able to make the web request in 
    ///     the first place. Otherwise there is danger of overwriting someone else's cookies.
    /// </SecurityNote>
    [SecurityCritical]
    internal static void HandleWebResponse(WebResponse response)
    {
        HttpWebResponse httpResponse = response as HttpWebResponse;
        if (httpResponse != null)
        {
            // Not relying on httpResponse.Cookies, because the original cookie header is needed, with all
            // attributes. (A CookieCollection can be stuffed in a CookieContainer, but CookieContainer.
            // GetCookieHeader() returns only name=value pairs.)
            WebHeaderCollection headers = httpResponse.Headers;
            // Further complication: headers["Set-cookie"] returns all cookies comma-separated. Splitting them
            // is not trivial, because expiration dates have commas. 
            // Plan B fails too: headers.GetValues("Set-Cookie") returns the cookies broken: It does some 
            // "normalization" and munging and apparently confuses the commas in cookie expiration dates for
            // cookie separators... 
            // The working solution is to find the index of the header and get all individual raw values 
            // associated with it. (WebHeaderCollection's internal storage is a string->ArrayList(of string) map.)
            for (int i = headers.Count-1; i >= 0; i--)
            {
                if (string.Compare(headers.Keys[i], "Set-Cookie", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string p3pHeader = httpResponse.Headers["P3P"];
                    foreach (string cookie in headers.GetValues(i))
                    {
                        try
                        {
                            SetCookieUnsafe(httpResponse.ResponseUri, cookie, p3pHeader);
                        }
                        catch (Exception ex) // A malformed cookie shouldn't fail the whole web request.
                        {
                            if (CriticalExceptions.IsCriticalException(ex))
                                throw;
                        }
                    }

                    break;
                }
            }
        }
    }

    /// <SecurityNote>
    /// Critical: Calls the native InternetGetCookieEx(). There is potential for information disclosure.
    /// Safe: A WebPermission demand is made for the given URI.
    /// </SecurityNote>
    [SecurityCritical, SecurityTreatAsSafe]
    [FriendAccessAllowed] // called by PF.Application.GetCookie()
    [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", 
        Justification="It's okay now. Be careful on change.")]
    internal static string GetCookie(Uri uri, bool throwIfNoCookie)
    {
        // Always demand in order to prevent any cross-domain information leak.
        SecurityHelper.DemandWebPermission(uri);

        UInt32 size = 0;
        string uriString = BindUriHelper.UriToString(uri);
        if (UnsafeNativeMethods.InternetGetCookieEx(uriString, null, null, ref size, 0, IntPtr.Zero))
        {
            Debug.Assert(size > 0);
            size++;
            System.Text.StringBuilder sb = new System.Text.StringBuilder((int)size);
            // PresentationHost intercepts InternetGetCookieEx(). It will set the INTERNET_COOKIE_THIRD_PARTY
            // flag if necessary.
            if (UnsafeNativeMethods.InternetGetCookieEx(uriString, null, sb, ref size, 0, IntPtr.Zero))
            {
                return sb.ToString();
            }
        }
        if (!throwIfNoCookie && Marshal.GetLastWin32Error() == NativeMethods.ERROR_NO_MORE_ITEMS)
            return null;
        throw new Win32Exception(/*uses last error code*/);
    }

    /// <SecurityNote>
    /// Critical: Calls SetCookieUnsafe().
    /// Safe: A WebPermission is demanded for the cookie URI, and no P3P header is passed.
    /// </SecurityNote>
    [SecurityCritical, SecurityTreatAsSafe]
    [FriendAccessAllowed] // called by PF.Application.SetCookie()
    internal static bool SetCookie(Uri uri, string cookieData)
    {
        SecurityHelper.DemandWebPermission(uri);

        return SetCookieUnsafe(uri, cookieData, null);
    }

    /// <SecurityNote>
    /// Critical: Sets cookies via the native InternetSetCookieEx(); doesn't demand WebPermission for the given
    ///     URI. This creates danger of overwriting someone else's cookies. 
    ///     The P3P header has to be from an authentic web response in order to be trusted at all.
    /// </SecurityNote>
    [SecurityCritical]
    private static bool SetCookieUnsafe(Uri uri, string cookieData, string p3pHeader)
    {
        string uriString = BindUriHelper.UriToString(uri);
        // PresentationHost intercepts InternetSetCookieEx(). It will set the INTERNET_COOKIE_THIRD_PARTY
        // flag if necessary. (This doesn't look very elegant but is much simpler than having to make the 
        // 3rd party decision here as well or calling into the native code (from PresentationCore).)
        uint res = UnsafeNativeMethods.InternetSetCookieEx(
            uriString, null, cookieData, UnsafeNativeMethods.INTERNET_COOKIE_EVALUATE_P3P, p3pHeader);
        if(res == 0)
            throw new Win32Exception(/*uses last error code*/);
        return res != UnsafeNativeMethods.COOKIE_STATE_REJECT;
    }

};

}
