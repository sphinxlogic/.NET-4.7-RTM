// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 

//
// KeyedHashAlgorithm.cs
//

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class KeyedHashAlgorithm : HashAlgorithm {
        protected byte[] KeyValue;

        protected KeyedHashAlgorithm() {}

        // IDisposable methods
        protected override void Dispose(bool disposing) {
            // For keyed hash algorithms, we always want to zero out the key value
            if (disposing) {
                if (KeyValue != null)
                    Array.Clear(KeyValue, 0, KeyValue.Length);
                KeyValue = null;
            }
            base.Dispose(disposing);
        }

        //
        // public properties
        //

        public virtual byte[] Key {
            get { return (byte[]) KeyValue.Clone(); }
            set {
                if (State != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashKeySet"));
                KeyValue = (byte[]) value.Clone();
            }
        }

        //
        // public methods
        //

        new static public KeyedHashAlgorithm Create() {
            return Create("System.Security.Cryptography.KeyedHashAlgorithm");
        }

        new static public KeyedHashAlgorithm Create(String algName) {
            return (KeyedHashAlgorithm) CryptoConfig.CreateFromName(algName);    
        }
    }
}
