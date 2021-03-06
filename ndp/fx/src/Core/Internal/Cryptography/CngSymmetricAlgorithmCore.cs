// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for crypto symmetric algorithms that use Cng.
    //
    internal struct CngSymmetricAlgorithmCore
    {
        /// <summary>
        /// Configures the core to use plaintext keys (to be auto-generated when first needed.)
        /// </summary>
        public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer)
        {
            _outer = outer;

            _keyName = null; // Setting _keyName to null signifies that this object is based on a plaintext key, not a stored CNG key.
            _provider = null;
            _optionOptions = CngKeyOpenOptions.None;
        }

        /// <summary>
        /// Constructs the core to use a stored CNG key. 
        /// </summary>
        public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer, string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
        {
            if (keyName == null)
                throw new ArgumentNullException("keyName");
            if (provider == null)
                throw new ArgumentNullException("provider");

            _outer = outer;

            _keyName = keyName;
            _provider = provider;
            _optionOptions = openOptions;

            using (CngKey cngKey = ProduceCngKey())
            {
                CngAlgorithm actualAlgorithm = cngKey.Algorithm;
                string algorithm = _outer.GetNCryptAlgorithmIdentifier();

                if (algorithm != actualAlgorithm.Algorithm)
                    throw new CryptographicException(SR.GetString(SR.Cryptography_CngKeyWrongAlgorithm, actualAlgorithm.Algorithm, algorithm));

                _outer.BaseKeySize = cngKey.KeySize;
            }
        }

        /// <summary>
        /// Note! This can and likely will throw if the algorithm was given a hardware-based key.
        /// </summary>
        public byte[] GetKeyIfExportable()
        {
            if (KeyInPlainText)
            {
                return _outer.BaseKey;
            }
            else
            {
                using (CngKey cngKey = ProduceCngKey())
                {
                    return cngKey.GetSymmetricKeyDataIfExportable(_outer.GetNCryptAlgorithmIdentifier());
                }
            }
        }

        public void SetKey(byte[] key)
        {
            _outer.BaseKey = key;
            _keyName = null; // Setting _keyName to null signifies that this object is now based on a plaintext key, not a stored CNG key.
        }

        public void SetKeySize(int keySize, ICngSymmetricAlgorithm outer)
        {
            // Warning: This gets invoked once before "this" is initialized, due to Aes(), DES(), etc., setting the KeySize property in their
            // nullary constructor. That's why we require "outer" being passed as parameter.
            Debug.Assert(_outer == null || _outer == outer);

            outer.BaseKeySize = keySize;
            _keyName = null; // Setting _keyName to null signifies that this object is now based on a plaintext key, not a stored CNG key.
        }

        public void GenerateKey()
        {
            byte[] key = Helpers.GenerateRandom(_outer.BaseKeySize.BitSizeToByteSize());
            SetKey(key);
        }

        public void GenerateIV()
        {
            byte[] iv = Helpers.GenerateRandom(_outer.BlockSize.BitSizeToByteSize());
            _outer.IV = iv;
        }

        public ICryptoTransform CreateEncryptor()
        {
            return CreateCryptoTransform(encrypting: true);
        }

        public ICryptoTransform CreateDecryptor()
        {
            return CreateCryptoTransform(encrypting: false);
        }

        public ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateCryptoTransform(rgbKey, rgbIV, encrypting: true);
        }

        public ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateCryptoTransform(rgbKey, rgbIV, encrypting: false);
        }

        private ICryptoTransform CreateCryptoTransform(bool encrypting)
        {
            if (KeyInPlainText)
            {
                return CreateCryptoTransform(_outer.BaseKey, _outer.IV, encrypting);
            }

            return CreatePersistedCryptoTransformCore(ProduceCngKey, _outer.IV, encrypting);
        }

        private ICryptoTransform CreateCryptoTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("key");

            byte[] key = rgbKey.CloneByteArray();

            long keySize = key.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(_outer.LegalKeySizes))
                throw new ArgumentException(SR.GetString(SR.Cryptography_InvalidKeySize, "key"));

            if (_outer.IsWeakKey(key))
                throw new CryptographicException(SR.GetString(SR.Cryptography_WeakKey));

            if (rgbIV != null && rgbIV.Length != _outer.BlockSize.BitSizeToByteSize())
                throw new ArgumentException(SR.GetString(SR.Cryptography_InvalidIVSize, "iv"));

            byte[] iv = _outer.Mode.GetCipherIv(rgbIV).CloneByteArray();

            if (LocalAppContextSwitches.SymmetricCngAlwaysUseNCrypt)
            {
                string algorithm = _outer.GetNCryptAlgorithmIdentifier();
                return CreatePersistedCryptoTransformCore(() => key.ToCngKey(algorithm), iv, encrypting);
            }

            return CreateEphemeralCryptoTransformCore(key, iv, encrypting);
        }

        private ICryptoTransform CreatePersistedCryptoTransformCore(Func<CngKey> cngKeyFactory, byte[] iv, bool encrypting)
        {
            int blockSizeInBytes = _outer.BlockSize.BitSizeToByteSize();
            BasicSymmetricCipher cipher = new BasicSymmetricCipherNCrypt(cngKeyFactory, _outer.Mode, blockSizeInBytes, iv, encrypting);
            return UniversalCryptoTransform.Create(_outer.Padding, cipher, encrypting);
        }

        [SecuritySafeCritical]
        private ICryptoTransform CreateEphemeralCryptoTransformCore(byte[] key, byte[] iv, bool encrypting)
        {
            int blockSizeInBytes = _outer.BlockSize.BitSizeToByteSize();
            SafeBCryptAlgorithmHandle algorithmModeHandle = _outer.GetEphemeralModeHandle();

            BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(
                algorithmModeHandle,
                _outer.Mode,
                blockSizeInBytes,
                key,
                iv,
                encrypting);

            return UniversalCryptoTransform.Create(_outer.Padding, cipher, encrypting);
        }

        private CngKey ProduceCngKey()
        {
            Debug.Assert(!KeyInPlainText);

            return CngKey.Open(_keyName, _provider, _optionOptions);
        }

        private bool KeyInPlainText
        {
            get { return _keyName == null; }
        }

        private readonly ICngSymmetricAlgorithm _outer;

        // If using a stored CNG key, these fields provide the CngKey.Open() parameters. If using a plaintext key, _keyName is set to null.
        private string _keyName;
        private CngProvider _provider;
        private CngKeyOpenOptions _optionOptions;

        private const int BitsPerByte = 8;
    }
}

