using System;
using MS.Internal;
using MS.Internal.WindowsBase;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;

//****** 
// Keep in sync with host\Inc\Registry.hxx

namespace MS.Internal
{
    [FriendAccessAllowed]
    internal static class RegistryKeys
    {
        internal const string
            WPF = @"Software\Microsoft\.NETFramework\Windows Presentation Foundation",

            WPF_Features = WPF+"\\Features",
                value_MediaImageDisallow = "MediaImageDisallow",
                value_MediaVideoDisallow = "MediaVideoDisallow",
                value_MediaAudioDisallow = "MediaAudioDisallow",
                value_WebBrowserDisallow = "WebBrowserDisallow",
                value_ScriptInteropDisallow = "ScriptInteropDisallow",
                value_AutomationWeakReferenceDisallow = "AutomationWeakReferenceDisallow",

            WPF_Hosting = WPF+"\\Hosting",
                value_DisableXbapErrorPage = "DisableXbapErrorPage",
                value_UnblockWebBrowserControl = "UnblockWebBrowserControl",

            HKCU_XpsViewer = @"HKEY_CURRENT_USER\Software\Microsoft\XPSViewer",
                value_IsolatedStorageUserQuota = "IsolatedStorageUserQuota",
            HKLM_XpsViewerLocalServer32 = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{7DDA204B-2097-47C9-8323-C40BB840AE44}\\LocalServer32",

            HKLM_IetfLanguage = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Nls\IetfLanguage",
            // These constants are cloned in 
            // wpf\src\Shared\Cpp\Utils.cxx
            // Should these reg keys change the above file should be also modified to reflect that.
            FRAMEWORK_RegKey  = @"Software\Microsoft\Net Framework Setup\NDP\v4\Client\",
            FRAMEWORK_RegKey_FullPath  = @"HKEY_LOCAL_MACHINE\" + FRAMEWORK_RegKey,
            FRAMEWORK_InstallPath_RegValue = "InstallPath";

        /// <SecurityNote>
        /// Critical: Reads an arbitrary registry key.
        /// </SecurityNote>
        [SecurityCritical]
        internal static bool ReadLocalMachineBool(string key, string valueName)
        {
            string keyPath = "HKEY_LOCAL_MACHINE\\" + key;
            new RegistryPermission(RegistryPermissionAccess.Read, keyPath).Assert();
            object value = Registry.GetValue(keyPath, valueName, null);
            return value is int && (int)value != 0;
        }
    };
}
