// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 

//
// X509UI.cs
//

namespace System.Security.Cryptography.X509Certificates {
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Permissions;

    public enum X509SelectionFlag {
        SingleSelection = 0x00,
        MultiSelection  = 0x01
    }

    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public static class X509Certificate2UI {
        [SecuritySafeCritical]
        public static void DisplayCertificate (X509Certificate2 certificate) {
            if (certificate == null)
                throw new ArgumentNullException("certificate");
            DisplayX509Certificate(X509Utils.GetCertContext(certificate), IntPtr.Zero);
        }

        [SecurityCritical]
        public static void DisplayCertificate (X509Certificate2 certificate, IntPtr hwndParent) {
            if (certificate == null)
                throw new ArgumentNullException("certificate");
            DisplayX509Certificate(X509Utils.GetCertContext(certificate), hwndParent);
        }

        public static X509Certificate2Collection SelectFromCollection (X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag) {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, IntPtr.Zero);
        }

        [SecurityCritical]
        public static X509Certificate2Collection SelectFromCollection (X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag, IntPtr hwndParent) {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, hwndParent);
        }

        [SecurityCritical]
        private static void DisplayX509Certificate (SafeCertContextHandle safeCertContext, IntPtr hwndParent) {
            if (safeCertContext.IsInvalid)
                throw new CryptographicException(SecurityResources.GetResourceString("Cryptography_InvalidHandle"), "safeCertContext");

            int dwErrorCode = CAPI.ERROR_SUCCESS;

            // Initialize view structure.
            CAPI.CRYPTUI_VIEWCERTIFICATE_STRUCTW ViewInfo = new CAPI.CRYPTUI_VIEWCERTIFICATE_STRUCTW();
            ViewInfo.dwSize = (uint) Marshal.SizeOf(ViewInfo);
            ViewInfo.hwndParent = hwndParent;
            ViewInfo.dwFlags = 0;
            ViewInfo.szTitle = null;
            ViewInfo.pCertContext = safeCertContext.DangerousGetHandle();
            ViewInfo.rgszPurposes = IntPtr.Zero;
            ViewInfo.cPurposes = 0;
            ViewInfo.pCryptProviderData = IntPtr.Zero;
            ViewInfo.fpCryptProviderDataTrustedUsage = false;
            ViewInfo.idxSigner = 0;
            ViewInfo.idxCert = 0;
            ViewInfo.fCounterSigner = false;
            ViewInfo.idxCounterSigner = 0;
            ViewInfo.cStores = 0;
            ViewInfo.rghStores = IntPtr.Zero;
            ViewInfo.cPropSheetPages = 0;
            ViewInfo.rgPropSheetPages = IntPtr.Zero;
            ViewInfo.nStartPage = 0;

            // View the certificate
            if (!CAPI.CryptUIDlgViewCertificateW(ViewInfo, IntPtr.Zero))
                dwErrorCode = Marshal.GetLastWin32Error();

            // CryptUIDlgViewCertificateW returns ERROR_CANCELLED if the user closes
            // the window through the x button or by pressing CANCEL, so ignore this error code
            if (dwErrorCode != CAPI.ERROR_SUCCESS && dwErrorCode != CAPI.ERROR_CANCELLED)  
                throw new CryptographicException(Marshal.GetLastWin32Error());
        }

        [SecuritySafeCritical]
        private static X509Certificate2Collection SelectFromCollectionHelper (X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag, IntPtr hwndParent) {
            if (certificates == null)
                throw new ArgumentNullException("certificates");
            if (selectionFlag < X509SelectionFlag.SingleSelection || selectionFlag > X509SelectionFlag.MultiSelection)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SecurityResources.GetResourceString("Arg_EnumIllegalVal"), "selectionFlag"));

            //
            // We need to Assert all StorePermission flags since this is a memory store and we want 
            // semi-trusted code to be able to select certificates from a memory store.
            //

            StorePermission sp = new StorePermission(StorePermissionFlags.AllFlags);
            sp.Assert();

            using (SafeCertStoreHandle safeSourceStoreHandle = X509Utils.ExportToMemoryStore(certificates))
            using (SafeCertStoreHandle safeTargetStoreHandle = SelectFromStore(safeSourceStoreHandle, title, message, selectionFlag, hwndParent))
            {
                return X509Utils.GetCertificates(safeTargetStoreHandle);
            }
        }

        [SecurityCritical]
        private static unsafe SafeCertStoreHandle SelectFromStore (SafeCertStoreHandle safeSourceStoreHandle, string title, string message, X509SelectionFlag selectionFlags, IntPtr hwndParent) {
            int dwErrorCode = CAPI.ERROR_SUCCESS;

            // First, create a memory store
            SafeCertStoreHandle safeCertStoreHandle = CAPI.CertOpenStore((IntPtr) CAPI.CERT_STORE_PROV_MEMORY, 
                                                                         CAPI.X509_ASN_ENCODING | CAPI.PKCS_7_ASN_ENCODING, 
                                                                         IntPtr.Zero, 
                                                                         0, 
                                                                         null);

            if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            CAPI.CRYPTUI_SELECTCERTIFICATE_STRUCTW csc = new CAPI.CRYPTUI_SELECTCERTIFICATE_STRUCTW();
            // Older versions of CRYPTUI do not check the size correctly,
            // so always force it to the oldest version of the structure.
            csc.dwSize = (uint) Marshal.OffsetOf(typeof(CAPI.CRYPTUI_SELECTCERTIFICATE_STRUCTW), "hSelectedCertStore");
            csc.hwndParent = hwndParent;
            csc.dwFlags = (uint) selectionFlags;
            csc.szTitle = title;
            csc.dwDontUseColumn = 0;
            csc.szDisplayString = message;
            csc.pFilterCallback = IntPtr.Zero;
            csc.pDisplayCallback = IntPtr.Zero;
            csc.pvCallbackData = IntPtr.Zero;
            csc.cDisplayStores = 1;
            IntPtr hSourceCertStore = safeSourceStoreHandle.DangerousGetHandle();
            csc.rghDisplayStores = new IntPtr(&hSourceCertStore);
            csc.cStores = 0;
            csc.rghStores = IntPtr.Zero;
            csc.cPropSheetPages = 0;
            csc.rgPropSheetPages = IntPtr.Zero;
            csc.hSelectedCertStore = safeCertStoreHandle.DangerousGetHandle();

            SafeCertContextHandle safeCertContextHandle = CAPI.CryptUIDlgSelectCertificateW(csc);

            if (safeCertContextHandle != null && !safeCertContextHandle.IsInvalid) {
                // Single select, so add it to our hCertStore
                SafeCertContextHandle ppStoreContext = SafeCertContextHandle.InvalidHandle;
                if (!CAPI.CertAddCertificateLinkToStore(safeCertStoreHandle, 
                                                        safeCertContextHandle, 
                                                        CAPI.CERT_STORE_ADD_ALWAYS, 
                                                        ppStoreContext))
                    dwErrorCode = Marshal.GetLastWin32Error();
            }

            if (dwErrorCode != CAPI.ERROR_SUCCESS)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            return safeCertStoreHandle;
        }
    }
}
