// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 

//
// SHA512.cs
//
// This abstract class represents the SHA-512 hash algorithm.
//

namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SHA512 : HashAlgorithm
    {
        //
        // protected constructors
        //

        protected SHA512() {
            HashSizeValue = 512;
        }

        //
        // public methods
        //

        new static public SHA512 Create() {
            return Create("System.Security.Cryptography.SHA512");
        }

        new static public SHA512 Create(String hashName) {
            return (SHA512) CryptoConfig.CreateFromName(hashName);
        }
    }
}

