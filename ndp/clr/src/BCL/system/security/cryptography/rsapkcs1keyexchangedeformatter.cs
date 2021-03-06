using System.Diagnostics.Contracts;
// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter {
        RSA _rsaKey;
        bool?  _rsaOverridesDecrypt;
        RandomNumberGenerator RngValue;

        // Constructors

        public RSAPKCS1KeyExchangeDeformatter() {}

        public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
        }

        //
        // public properties
        //

        public RandomNumberGenerator RNG {
            get { return RngValue; }
            set { RngValue = value; }
        }
        
        public override String Parameters {
            get { return null; }
            set { ;}
        }

        //
        // public methods
        //

        public override byte[] DecryptKeyExchange(byte[] rgbIn) {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));

            byte[] rgbOut;
            if (OverridesDecrypt) {
                rgbOut = _rsaKey.Decrypt(rgbIn, RSAEncryptionPadding.Pkcs1);
            }
            else {
                int i;
                byte[] rgb;
                rgb = _rsaKey.DecryptValue(rgbIn);

                //
                //  Expected format is:
                //      00 || 02 || PS || 00 || D
                //      where PS does not contain any zeros.
                //

                for (i = 2; i<rgb.Length; i++) {
                    if (rgb[i] == 0) {
                        break;
                    }
                }

                if (i >= rgb.Length)
                    throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_PKCS1Decoding"));

                i++;            // Skip over the zero

                rgbOut = new byte[rgb.Length - i];
                Buffer.InternalBlockCopy(rgb, i, rgbOut, 0, rgbOut.Length);
            }
            return rgbOut;
        }

        public override void SetKey(AsymmetricAlgorithm key) {
            if (key == null) 
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA) key;
            _rsaOverridesDecrypt = default(bool?);
        }

        private bool OverridesDecrypt {
            get {
                if (!_rsaOverridesDecrypt.HasValue) {
                    _rsaOverridesDecrypt = Utils.DoesRsaKeyOverride(_rsaKey, "Decrypt", new Type[] { typeof(byte[]), typeof(RSAEncryptionPadding) });
                }
                return _rsaOverridesDecrypt.Value;
            }
        }
    }
}
