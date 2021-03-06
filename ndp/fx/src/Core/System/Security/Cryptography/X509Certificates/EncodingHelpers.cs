// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class EncodingHelpers
    {
        internal static readonly byte[] s_emptyArray = new byte[0];

        internal static byte[][] WrapAsSegmentedForSequence(this byte[] derData)
        {
            // ConstructSegmentedSequence understands triplets, but doesn't care they're valid,
            // so lift this up into a "triplet".
            return new[] { s_emptyArray, s_emptyArray, derData };
        }

        internal static void ValidateSignatureAlgorithm(byte[] signatureAlgorithm)
        {
            Debug.Assert(signatureAlgorithm != null);

            // AlgorithmIdentifier ::= SEQUENCE { OBJECT IDENTIFIER, ANY }
            DerSequenceReader validator = new DerSequenceReader(signatureAlgorithm);
            validator.ReadOidAsString();

            if (validator.HasData)
            {
                validator.ValidateAndSkipDerValue();
            }

            if (validator.HasData)
                throw new CryptographicException(SR.GetString(SR.Cryptography_Der_Invalid_Encoding));
        }

        internal static byte[][] SegmentedEncodeSubjectPublicKeyInfo(this PublicKey publicKey)
        {
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));

            if (publicKey.Oid == null ||
                string.IsNullOrEmpty(publicKey.Oid.Value) ||
                publicKey.EncodedKeyValue == null)
            {
                throw new CryptographicException(SR.GetString(SR.Cryptography_InvalidPublicKey_Object));
            }

            // SubjectPublicKeyInfo::= SEQUENCE  {
            //   algorithm AlgorithmIdentifier,
            //   subjectPublicKey     BIT STRING
            // }
            //
            // AlgorithmIdentifier::= SEQUENCE  {
            //   algorithm OBJECT IDENTIFIER,
            //   parameters ANY DEFINED BY algorithm OPTIONAL
            // }

            byte[][] algorithmIdentifier;

            if (publicKey.EncodedParameters == null)
            {
                algorithmIdentifier = DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.SegmentedEncodeOid(publicKey.Oid));
            }
            else
            {
                DerSequenceReader validator =
                    DerSequenceReader.CreateForPayload(publicKey.EncodedParameters.RawData);

                validator.ValidateAndSkipDerValue();

                if (validator.HasData)
                    throw new CryptographicException(SR.GetString(SR.Cryptography_Der_Invalid_Encoding));

                algorithmIdentifier = DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.SegmentedEncodeOid(publicKey.Oid),
                    publicKey.EncodedParameters.RawData.WrapAsSegmentedForSequence());
            }

            return DerEncoder.ConstructSegmentedSequence(
                algorithmIdentifier,
                DerEncoder.SegmentedEncodeBitString(
                    publicKey.EncodedKeyValue.RawData));
        }

        internal static byte[][] SegmentedEncodedX509Extension(this X509Extension extension)
        {
            if (extension.Critical)
            {
                return DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.SegmentedEncodeOid(extension.Oid),
                    DerEncoder.SegmentedEncodeBoolean(extension.Critical),
                    DerEncoder.SegmentedEncodeOctetString(extension.RawData));
            }

            return DerEncoder.ConstructSegmentedSequence(
                DerEncoder.SegmentedEncodeOid(extension.Oid),
                DerEncoder.SegmentedEncodeOctetString(extension.RawData));
        }

        internal static byte[][] SegmentedEncodeAttributeSet(this IEnumerable<X501Attribute> attributes)
        {
            List<byte[][]> encodedAttributes = new List<byte[][]>();

            foreach (X501Attribute attribute in attributes)
            {
                encodedAttributes.Add(
                    DerEncoder.ConstructSegmentedSequence(
                        DerEncoder.SegmentedEncodeOid(attribute.Oid),
                        DerEncoder.ConstructSegmentedPresortedSet(
                            attribute.RawData.WrapAsSegmentedForSequence())));
            }

            return DerEncoder.ConstructSegmentedSet(encodedAttributes.ToArray());
        }
    }
}
