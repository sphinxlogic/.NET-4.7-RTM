//------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  Description:
//     Implements the interface to the application manifest
//
// History:
//      2005/05/09 - Microsoft     Created
//      2007/09/20   Microsoft     Ported Windows->DevDiv. See SourcesHistory.txt.
//
//------------------------------------------------------------------------

#include "PreCompiled.hxx"
#include "MarkupVersion.hxx"
#include "..\inc\registry.hxx"


#define COMPATURL L"http://schemas.openxmlformats.org/markup-compatibility/2006"
#define COMPATURL_LENGTH 63

#define IGNORABLE L"Ignorable"
#define IGNORABLE_LENGTH 9

#define MAX_PREFIX_LENGTH 128

CMarkupVersion::CMarkupVersion(__in LPCWSTR pswzLocalMarkupPath)
{
    SetLocalMarkupPath(pswzLocalMarkupPath);
    m_refCount = 0;
}

STDMETHODIMP CMarkupVersion::QueryInterface(const struct _GUID &riid,void ** ppvObject)
{
    *ppvObject = NULL;

    if (riid == IID_IUnknown)
    {
        *ppvObject = static_cast<ISAXContentHandler *>(this);
    }
    else if (riid == __uuidof(ISAXContentHandler))
    {
        *ppvObject = static_cast<ISAXContentHandler *>(this);
    }

    if (*ppvObject)
    {
        AddRef();
        return S_OK;
    }    
    else 
    {
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(DWORD) CMarkupVersion::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

STDMETHODIMP_(DWORD) CMarkupVersion::Release()
{
    InterlockedDecrement(&m_refCount);
    if (m_refCount == 0) 
    {
        delete this;
        return 0;
    }
    else 
    {
        return m_refCount;
    }
}

IFACEMETHODIMP CMarkupVersion::startPrefixMapping(
    __in const wchar_t*   pwchPrefix,
    __in int              /*cchPrefix*/,
    __in const wchar_t*   pwchUri,
    __in int              /*cchUri*/)
{
    HRESULT hr = S_OK;

    // Accumulate and record the namespaces

    // See if it is a namespace that we know.
    CString* strVersion = NULL;
    if (SUCCEEDED(m_mapNamespaceVersion.Find(pwchUri, &strVersion)))
    {
        CString *pUriString = CString::CreateOnHeap(pwchUri);
        CK_ALLOC(pUriString);
        CKHR(m_mapPrefixNamespace.Add(pwchPrefix, pUriString));
    }

Cleanup:
    return hr;
}

IFACEMETHODIMP CMarkupVersion::startElement(
    __in_ecount(cchNamespaceUri) const wchar_t *pwchNamespaceUri,
    __in int cchNamespaceUri,
    __in_ecount(cchLocalName) const wchar_t *pwchLocalName,
    __in int cchLocalName,
    __in_ecount(cchQName) const wchar_t *pwchQName,
    __in int cchQName,
    __in ISAXAttributes *pAttributes)
{
    HRESULT hr = S_OK;

    const UINT BUFFER_LENGTH = 1024;
    LPCWSTR pwzValue;

    // This retrieves a space-delimited list of the ignorable prefices.
    // If there is some error finding the attribute, or if it wasn't there, we don't care.
    // Returning a failed HRESULT will stop the parsing. The namespaces have already been 
    // reported in startPrefixMapping.
    int nLength = BUFFER_LENGTH;
    CKHR(pAttributes->getValueFromName(COMPATURL, COMPATURL_LENGTH, IGNORABLE, IGNORABLE_LENGTH, &pwzValue, &nLength));

    WCHAR wzPrefix[MAX_PREFIX_LENGTH];
    LPCWSTR pStart = pwzValue;
    while (*pStart)
    {
        // Remove any leading spaces
        while (*pStart && *pStart == L' ')
        {
            ++pStart;
        }

        // Get the prefix from the space-delimited list
        __bound UINT nIndex = 0;
        while (*pStart && *pStart != L' ' && nIndex < MAX_PREFIX_LENGTH - 1)
        {
            wzPrefix[nIndex++] = *(pStart++);
        }
        wzPrefix[nIndex] = 0;
     
        if (*wzPrefix)
        {
            CString* pStrNamespace = NULL;
            if (SUCCEEDED(m_mapPrefixNamespace.Find(wzPrefix, &pStrNamespace)))
            {
                // This is a namespace we know about
                CString* pStrVersion = NULL;
                if (SUCCEEDED(m_mapNamespaceVersion.Find(pStrNamespace->GetValue(), &pStrVersion)))
                {
                    CKHR(m_mapIgnorableNamespaceVersion.Add(pStrNamespace->GetValue(), pStrVersion));
                }
            }
        }
    }

    CKHR(E_FAIL); // to stop the parsing

Cleanup:
    return hr;
}

HRESULT CMarkupVersion::Read()
{
    HRESULT hr = S_OK;
    ISAXXMLReader* pReader = NULL;

    EventWriteWpfHostUm_ParsingMarkupVersionStart();

    CKHR(GetStringMapFromRegistry(HKEY_LOCAL_MACHINE, RegKey_WPF_Namespaces, m_mapNamespaceVersion));

    if (m_mapNamespaceVersion.GetCount() > 0)
    {
        CKHR(CoCreateInstance(__uuidof(SAXXMLReader60), NULL, CLSCTX_INPROC_SERVER, __uuidof(ISAXXMLReader), (void**)&pReader));
        CKHR(pReader->putContentHandler(this));
        hr = pReader->parseURL(GetLocalMarkupPath());

        // If we stopped the parse because we found the version, hr will be E_FAIL and
        // the version will be set in the manifest.
        if (hr == E_FAIL)
        {
            hr = S_OK;
        }
    }

    EventWriteWpfHostUm_ParsingMarkupVersionEnd();

Cleanup:
    SAFERELEASE_POINTER(pReader);

    return hr;
}
