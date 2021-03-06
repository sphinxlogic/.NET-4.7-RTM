#include <windows.h>
#include <shlwapi.h>
#include "Utils.hxx" // from shared\inc
#include "dwriteloader.h" // from shared\inc

// This is how these files are declared in truetype.cpp.
// They end up belonging to namespace MS::Internal::TtfDelta
// because truetype.cpp include the cpp files inside this namespace.
// We cannot simply put this namespace specification in these 2 header files
// or elase we will break the compilation of truetype subsetter.
namespace MS { namespace Internal { namespace TtfDelta { 
#include "CPP\TrueTypeSubsetter\TtfDelta\GlobalInit.h"
#include "CPP\TrueTypeSubsetter\TtfDelta\ControlTableInit.h"
}}} // namespace MS::Internal::TtfDelta

using namespace System;
using namespace System::ComponentModel;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security;
using namespace System::Security::Permissions;
using namespace System::Diagnostics;

[assembly:DependencyAttribute("System,", LoadHint::Always)];
[assembly:DependencyAttribute("WindowsBase,", LoadHint::Always)];

#ifndef ARRAYSIZE
#define ARRAYSIZE RTL_NUMBER_OF_V2 // from DevDiv's WinNT.h
#endif

//
// Add a module-level initialization code here.
//
// The constructor of below class should be called before any other
// code in this Assembly when the assembly is loaded into any AppDomain.
//

//
// We want to call SetProcessDPIAware from user32.dll only on machines
// running Vista or later OSs.  We provide our own declaration (the
// original is in winuser.h) here so we can specify the DllImport attribute
// which allows delayed loading of the function - thereby allowing us to
// run on pre-Vista OSs.
//

[DllImport("user32.dll", EntryPoint="SetProcessDPIAware")]
[SuppressUnmanagedCodeSecurity, SecurityCritical]
WINUSERAPI
BOOL
WINAPI
SetProcessDPIAware_Internal(
    VOID);


#define WINNT_VISTA_VERSION     0x06
#define WPFGFX_40_DLLNAME       L"wpfgfx_v0400.dll"
#define NATIVE_40_DLLNAME       L"PresentationNative_v0400.dll"

 namespace MS { namespace Internal {
private ref class NativeWPFDLLLoader sealed
{
public:
    //
    // Loads the wpfgfx and PresentationNative libraries from the version-specific installation folder.
    // This enables the CLR to resolve DllImport declarations for functions exported from these libraries.
    // The installation folder is not on the normal search path, so its location is found from the registry.
    //
    // <SecurityNote>
    // Critical -- Calls native method LoadLibrary from kernel32.dll.
    //
    // TreatAsSafe -- LoadLibrary is being passed a value from a installer-set registry key with a
    //                known library name, limiting the risk.
    //
    // </SecurityNote>
    [SecuritySafeCritical]
    [SecurityPermission(SecurityAction::Assert, UnmanagedCode=true)]
    static void LoadCommonDLLsAndDwrite( )
    {
        WCHAR wpfInstallPath[MAX_PATH];

        HRESULT hr = WPFUtils::GetWPFInstallPath(wpfInstallPath, ARRAYSIZE(wpfInstallPath));
        if (FAILED(hr))
            Marshal::ThrowExceptionForHR(hr);

        // We load dwrite here because it's cleanup logic is different from the other native dlls
        // and don't want to abstract that
        VOID *pTemp = NULL;
        m_hDWrite = System::IntPtr(WPFUtils::LoadDWriteLibraryAndGetProcAddress(&pTemp));
        if (m_hDWrite == IntPtr::Zero)
            throw gcnew DllNotFoundException(gcnew String(L"dwrite.dll"), gcnew Win32Exception());        
        if (pTemp == NULL)
            throw gcnew InvalidOperationException();
        m_pfnDWriteCreateFactory = pTemp;

        m_hWpfGfx             = LoadNativeWPFDLL(WPFGFX_40_DLLNAME, wpfInstallPath);
        m_hPresentationNative = LoadNativeWPFDLL(NATIVE_40_DLLNAME, wpfInstallPath);
    }
    
    // <SecurityNote>
    // Critical -- Calls critical FreeLibrary to unload a native library
    // TreatAsSafe -- Known\trusted handles to wpfgfx_v0400.dll and PresentationNative_v0400.dll are passed
    // </SecurityNote>    
    [SecuritySafeCritical]
     __declspec(noinline) 
    static void UnloadCommonDLLs()
    {
        if (m_hWpfGfx != IntPtr::Zero)
        {
            if (!FreeLibrary((HMODULE)(m_hWpfGfx.ToPointer())))
            {
                DWORD lastError = GetLastError();
                Marshal::ThrowExceptionForHR(__HRESULT_FROM_WIN32(lastError));
            }
            
            m_hWpfGfx = IntPtr::Zero;
        }
        
        if (m_hPresentationNative != IntPtr::Zero)
        {
            if (!FreeLibrary((HMODULE)(m_hPresentationNative.ToPointer())))
            {
                DWORD lastError = GetLastError();
                Marshal::ThrowExceptionForHR(__HRESULT_FROM_WIN32(lastError));
            }
            
            m_hPresentationNative = IntPtr::Zero;
        }
    }
    
    // <SecurityNote>
    // Critical -- Calls critical FreeLibrary to unload a native library
    // TreatAsSafe -- A known\trusted handle to dwrite.dll is passed
    // </SecurityNote>    
    [SecuritySafeCritical]
     __declspec(noinline) 
    static void UnloadDWrite()
    {
        ClearDWriteCreateFactoryFunctionPointer();
        
        if (m_hDWrite != IntPtr::Zero)
        {
            if (!FreeLibrary((HMODULE)(m_hDWrite.ToPointer())))
            {
                DWORD lastError = GetLastError();
                Marshal::ThrowExceptionForHR(__HRESULT_FROM_WIN32(lastError));
            }
                
            m_hDWrite = IntPtr::Zero;
        }
    }
    
    // <SecurityNote>
    // Critical -- Calls critical LoadNativeWPFDLL to load a native library
    // TreatAsSafe -- The path to the known\trusted PresentationNative_v0400.dll is passed
    // </SecurityNote>    
    [SecuritySafeCritical]
     __declspec(noinline) 
    static void LoadPresentationNative()
    {
        if (m_hPresentationNative == IntPtr::Zero)
        {
            WCHAR wpfInstallPath[MAX_PATH];
            HRESULT hr = WPFUtils::GetWPFInstallPath(wpfInstallPath, ARRAYSIZE(wpfInstallPath));
            if (FAILED(hr))
                Marshal::ThrowExceptionForHR(hr);
            
            m_hPresentationNative = LoadNativeWPFDLL(NATIVE_40_DLLNAME, wpfInstallPath);
        }
    }
    
    /// <SecurityNote>
    /// Critical: Exposes a pointer to the DWrite method that is used to create factories
    ///           which can be used to obtain any info about fonts.
    /// </SecurityNote>
    [SecurityCritical]
    static void *GetDWriteCreateFactoryFunctionPointer()
    {
        return m_pfnDWriteCreateFactory;
    }

    /// <SecurityNote>
    /// Critical: Nulls a pointer to the DWrite method that is used to create factories
    /// </SecurityNote>
    [SecurityCritical]
    static void ClearDWriteCreateFactoryFunctionPointer()
    {
        m_pfnDWriteCreateFactory = NULL;    
    }

private:    
    
    // <SecurityNote>
    // Critical -- Calls loads an arbitrary a native library
    //             It is possible to buffer overlow if relDllPath and baseDllPath concatonated exceed MAX_PATH
    // </SecurityNote>
    [SecurityCritical]
     __declspec(noinline) 
    static System::IntPtr LoadNativeWPFDLL(LPCWSTR relDllPath, LPCWSTR baseDllPath)
    {
        System::IntPtr result = IntPtr::Zero;
        WCHAR dllPath[MAX_PATH] = {};
        
#pragma prefast(suppress:25025, "We don't know of a better API to use in place of PathCombine. The OACR spreadsheet and MSDN do not suggest any either.")
            if (!::PathCombine(dllPath, baseDllPath, relDllPath))
                throw gcnew System::IO::PathTooLongException();
            result = System::IntPtr(LoadLibrary(dllPath));
            if (result == IntPtr::Zero)
                throw gcnew DllNotFoundException(gcnew String(dllPath), gcnew Win32Exception());
            
            return result;
    }
    
    static System::IntPtr m_hWpfGfx;
    static System::IntPtr m_hPresentationNative;
    static System::IntPtr m_hDWrite;
    
    // <SecurityNote>
    // Critical -- Field is untyped pointer
    // </SecurityNote>
    [SecurityCritical]
    static void *m_pfnDWriteCreateFactory;
}; 
}} // namespace MS.Internal
    
private class CModuleInitialize
{
public:

    // Constructor of class CModuleInitialize
    // <SecurityNote>
    // Critical -- Calls native methods atexit.
    //
    // TreatAsSafe -- The function passed to atexit is trusted.
    //
    // </SecurityNote>    
    [SecuritySafeCritical]
    __declspec(noinline) CModuleInitialize(void (*cleaningUpFunc)())
    {
        IsProcessDpiAware();
        MS::Internal::NativeWPFDLLLoader::LoadCommonDLLsAndDwrite();

        // Initialize some global arrays.
        MS::Internal::TtfDelta::GlobalInit::Init();
        MS::Internal::TtfDelta::ControlTableInit::Init();
        atexit(cleaningUpFunc);
    }

    /// <SecurityNote>
    /// Critical: Asserts UnmanagedCode permission to unload the native DLLs.
    /// Safe    : The libraries to be released are coming from internally 
    ///           trusted source
    /// </SecurityNote>
    [SecuritySafeCritical]
    [SecurityPermission(SecurityAction::Assert, UnmanagedCode=true)]
    // Previously we had this as a class dtor but we found out that
    // we can't use a destructor due to an issue with how it's registered to be called on exit:
    // A compiler-generated function calls _atexit_m_appdomain(). But that generated function is transparenct,
    // which causes a violation because _atexit_m_appdomain() is Critical.
    __declspec(noinline) void UnInitialize()
    {
        MS::Internal::NativeWPFDLLLoader::UnloadCommonDLLs();
        
        MS::Internal::NativeWPFDLLLoader::ClearDWriteCreateFactoryFunctionPointer();
        //
        // Finalizers run after this dtor so if we unload dwrite now
        // we may end up making calls into unloaded code. Yes, this 
        // is a "leak" but it's only really a leak if no more WPF 
        // AppDomains are present and it's a single leak since only
        // one instance of a version of a CLR may be in proc at 
        // once.
        //
        // We could also use a critical finalizer for the handle
        // but that requires changing this code quite a bit plus
        // if other critical finalizers ever call dwrite code
        // we have the same problem again.
        //
        // MS::Internal::NativeWPFDLLLoader::UnloadDWrite();
    }

    /// <SecurityNote>
    /// Critical: Exposes a pointer to the DWrite method that is used to create factories
    ///           which can be used to obtain any info about fonts.
    /// </SecurityNote>
    [SecurityCritical]
    void *GetDWriteCreateFactoryFunctionPointer()
    {
        return MS::Internal::NativeWPFDLLLoader::GetDWriteCreateFactoryFunctionPointer();
    }

private :

    //
    // A private helper method to handle the DpiAwareness issue for current application.
    // This method is set as noinline since the MC++ compiler may otherwise inline it in a 
    // Security Transparent method which will lead to a security violation where the transparent
    // method will be calling security critical code in this method.
    //
    // <SecurityNote>
    // Critical -- Calls native methods SetProcessDPIAware from user32.dll (via our own extern).
    //
    // TreatAsSafe -- There's nothing inherently risky about calling SetProcessDPIAware - it simply
    //                lets the OS know how to treat the visual display of the app.
    //
    // </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) void IsProcessDpiAware( )
    {
        Version  ^osVersion = (Environment::OSVersion)->Version;

        if (osVersion->Major < WINNT_VISTA_VERSION)
        {
            // DPIAware feature is available only in Vista and after.
            return;
        }

        //
        // Below code is only for Vista and newer platform.
        //
        Assembly ^ assemblyApp;
        Type ^  disableDpiAwareType = System::Windows::Media::DisableDpiAwarenessAttribute::typeid;
        bool    bDisableDpiAware = false;

        // By default, Application is DPIAware.
        assemblyApp = Assembly::GetEntryAssembly();

        // Check if the Application has explicitly set DisableDpiAwareness attribute.
        if (assemblyApp != nullptr && Attribute::IsDefined(assemblyApp, disableDpiAwareType))
        {
            bDisableDpiAware = true;
        }


        if (!bDisableDpiAware)
        {
            // DpiAware composition is enabled for this application.
            SetProcessDPIAware_Internal( );
        }

        // Only when DisableDpiAwareness attribute is set in Application assembly,
        // It will ignore the SetProcessDPIAware API call.
    }

};

void CleanUp();

/// <summary>
/// This method is a workaround to an ugly bug in the compiler that caused Jitting.
/// The compiler generates a static unsafe method to initialize cmiStartupRunner
/// which is not properly annotated with security tags.
/// To work around this issue we create our own static method that is properly annotated.
/// </summary>
/// <SecurityNote>
/// Critical: Contains unverifiable native code.
/// Safe    : The code is safe and only returns a new object.
/// </SecurityNote>
[SecuritySafeCritical]
__declspec(noinline) static System::IntPtr CreateCModuleInitialize()
{
    return System::IntPtr(new CModuleInitialize(CleanUp));
}

// Important Note: This variable is declared as System::IntPtr to fool the compiler into creating
// a safe static method that initialzes it. If this variable was declared as CModuleInitialize
// Then the generated method is unsafe, fails NGENing and causes Jitting.
__declspec(appdomain) static System::IntPtr cmiStartupRunner = CreateCModuleInitialize();

[SecuritySafeCritical]
void CleanUp()
{
    CModuleInitialize* pCmiStartupRunner = static_cast<CModuleInitialize*>(cmiStartupRunner.ToPointer());

    pCmiStartupRunner->UnInitialize();
    delete pCmiStartupRunner;
    cmiStartupRunner = System::IntPtr(NULL);

}

/// <SecurityNote>
/// Critical: Exposes a pointer to the DWrite method that is used to create factories
///           which can be used to obtain any info about fonts.
/// </SecurityNote>
[SecurityCritical]
void *GetDWriteCreateFactoryFunctionPointer()
{
    return (static_cast<CModuleInitialize*>(cmiStartupRunner.ToPointer()))->GetDWriteCreateFactoryFunctionPointer();
}

