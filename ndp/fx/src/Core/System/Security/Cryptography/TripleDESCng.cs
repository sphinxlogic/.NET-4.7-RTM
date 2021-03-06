// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

//
// This file is one of a group of files (AesCng.cs, TripleDESCng.cs) that are almost identical except
// for the algorithm name. If you make a change to this file, there's a good chance you'll have to make
// the same change to the other files so please check. This is a pain but given that the contracts demand
// that each of these derive from a different class, it can't be helped.
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class TripleDESCng : TripleDES, ICngSymmetricAlgorithm
    {
        public TripleDESCng()
        {
            SetLegalKeySizesValue();
            _core = new CngSymmetricAlgorithmCore(this);
        }

        public TripleDESCng(string keyName)
            : this(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider)
        {
        }

        public TripleDESCng(string keyName, CngProvider provider)
            : this(keyName, provider, CngKeyOpenOptions.None)
        {
        }

        public TripleDESCng(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            SetLegalKeySizesValue();
            _core = new CngSymmetricAlgorithmCore(this, keyName, provider, openOptions);
        }

        public override byte[] Key
        {
            get
            {
                return _core.GetKeyIfExportable();
            }

            set
            {
                _core.SetKey(value);
            }
        }

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }

            set
            {
                _core.SetKeySize(value, this);
            }
        }

        public override ICryptoTransform CreateDecryptor()
        {
            // Do not change to CreateDecryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateDecryptor();
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return _core.CreateDecryptor(rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            // Do not change to CreateEncryptor(this.Key, this.IV). this.Key throws if a non-exportable hardware key is being used.
            return _core.CreateEncryptor();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return _core.CreateEncryptor(rgbKey, rgbIV);
        }
 
        public override void GenerateKey()
        {
            _core.GenerateKey();
        }

        public override void GenerateIV()
        {
            _core.GenerateIV();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        byte[] ICngSymmetricAlgorithm.BaseKey { get { return base.Key; } set { base.Key = value; } }
        int ICngSymmetricAlgorithm.BaseKeySize { get { return base.KeySize; } set { base.KeySize = value; } }

        bool ICngSymmetricAlgorithm.IsWeakKey(byte[] key)
        {
            return TripleDES.IsWeakKey(key);
        }

        [SecurityCritical]
        SafeBCryptAlgorithmHandle ICngSymmetricAlgorithm.GetEphemeralModeHandle()
        {
            return BCryptNative.TripleDesBCryptModes.GetSharedHandle(Mode);
        }

        string ICngSymmetricAlgorithm.GetNCryptAlgorithmIdentifier()
        {
            return Interop.NCrypt.NCRYPT_3DES_ALGORITHM;
        }

        private void SetLegalKeySizesValue()
        {
            // CNG does not support 128-bit keys.
            LegalKeySizesValue = new KeySizes[] { new KeySizes(minSize: 3 * 64, maxSize: 3 * 64, skipSize: 0) };
        }

        private CngSymmetricAlgorithmCore _core;
    }
}
