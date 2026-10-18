using System;
using System.Security.Cryptography;
using UnityEngine;

namespace Harborlight
{
    internal static class HarborlightRsaKeyImport
    {
        private static readonly byte[] RsaEncryptionOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };

        public static RSAEncryptionPadding JoinTicketPadding
        {
            get
            {
                var platform = Application.platform;
                if (Debug.isDebugBuild && (platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer))
                    return RSAEncryptionPadding.OaepSHA1;
                return RSAEncryptionPadding.OaepSHA256;
            }
        }

        public static RSA CreateFromPkcs8PrivateKey(byte[] encodedKey)
        {
            var encoded = new DerReader(encodedKey);
            var version = new DerReader(encoded.ReadSequence());
            encoded.RequireEnd();
            version.ReadVersion(allowOne: false);
            var algorithm = new DerReader(version.ReadSequence());
            RequireRsaAlgorithm(algorithm);
            var privateKey = new DerReader(version.ReadOctetString());
            version.RequireEnd();

            var rsaSequence = new DerReader(privateKey.ReadSequence());
            rsaSequence.ReadVersion(allowOne: false);
            var parameters = new RSAParameters
            {
                Modulus = rsaSequence.ReadPositiveInteger(),
                Exponent = rsaSequence.ReadPositiveInteger(),
                D = rsaSequence.ReadPositiveInteger(),
                P = rsaSequence.ReadPositiveInteger(),
                Q = rsaSequence.ReadPositiveInteger(),
                DP = rsaSequence.ReadPositiveInteger(),
                DQ = rsaSequence.ReadPositiveInteger(),
                InverseQ = rsaSequence.ReadPositiveInteger()
            };
            rsaSequence.RequireEnd();
            privateKey.RequireEnd();

            return Create(parameters);
        }

        public static RSA CreateFromSubjectPublicKeyInfo(byte[] encodedKey)
        {
            var encoded = new DerReader(encodedKey);
            var info = new DerReader(encoded.ReadSequence());
            encoded.RequireEnd();
            var algorithm = new DerReader(info.ReadSequence());
            RequireRsaAlgorithm(algorithm);
            var bitString = info.ReadValue(0x03);
            info.RequireEnd();
            if (bitString.Length < 2 || bitString[0] != 0)
                throw new CryptographicException("Invalid RSA subject public key bit string.");

            var publicKey = new byte[bitString.Length - 1];
            Buffer.BlockCopy(bitString, 1, publicKey, 0, publicKey.Length);
            var encodedPublicKey = new DerReader(publicKey);
            var rsaSequence = new DerReader(encodedPublicKey.ReadSequence());
            encodedPublicKey.RequireEnd();
            var parameters = new RSAParameters
            {
                Modulus = rsaSequence.ReadPositiveInteger(),
                Exponent = rsaSequence.ReadPositiveInteger()
            };
            rsaSequence.RequireEnd();
            return Create(parameters);
        }

        private static RSA Create(RSAParameters parameters)
        {
            var rsa = RSA.Create();
            try
            {
                rsa.ImportParameters(parameters);
                if (rsa.KeySize < 2048)
                    throw new CryptographicException("Harborlight RSA keys must be at least 2048 bits.");
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        private static void RequireRsaAlgorithm(DerReader reader)
        {
            if (!Equal(reader.ReadValue(0x06), RsaEncryptionOid))
                throw new CryptographicException("The encoded key is not an RSA key.");
            if (!reader.AtEnd)
            {
                var nullParameters = reader.ReadValue(0x05);
                if (nullParameters.Length != 0)
                    throw new CryptographicException("Invalid RSA algorithm parameters.");
            }
            reader.RequireEnd();
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private sealed class DerReader
        {
            private readonly byte[] _data;
            private int _offset;

            public DerReader(byte[] data)
            {
                _data = data ?? throw new CryptographicException("RSA key data is missing.");
            }

            public bool AtEnd => _offset == _data.Length;

            public byte[] ReadSequence() => ReadValue(0x30);

            public byte[] ReadOctetString() => ReadValue(0x04);

            public byte[] ReadPositiveInteger()
            {
                var value = ReadValue(0x02);
                if (value.Length == 0 || (value[0] & 0x80) != 0)
                    throw new CryptographicException("RSA key integer must be positive.");

                var first = 0;
                while (first < value.Length - 1 && value[first] == 0) first++;
                var normalized = new byte[value.Length - first];
                Buffer.BlockCopy(value, first, normalized, 0, normalized.Length);
                return normalized;
            }

            public void ReadVersion(bool allowOne)
            {
                var version = ReadPositiveInteger();
                if (version.Length != 1 || (version[0] != 0 && (!allowOne || version[0] != 1)))
                    throw new CryptographicException("Unsupported RSA key version.");
            }

            public byte[] ReadValue(byte expectedTag)
            {
                if (_offset >= _data.Length || _data[_offset++] != expectedTag)
                    throw new CryptographicException("Unexpected RSA key encoding tag.");
                if (_offset >= _data.Length)
                    throw new CryptographicException("Truncated RSA key length.");

                var firstLength = _data[_offset++];
                int length;
                if ((firstLength & 0x80) == 0)
                {
                    length = firstLength;
                }
                else
                {
                    var lengthBytes = firstLength & 0x7f;
                    if (lengthBytes == 0 || lengthBytes > 4 || _offset + lengthBytes > _data.Length || _data[_offset] == 0)
                        throw new CryptographicException("Invalid RSA key length encoding.");
                    length = 0;
                    for (var i = 0; i < lengthBytes; i++)
                    {
                        if (length > (int.MaxValue >> 8))
                            throw new CryptographicException("RSA key length is out of range.");
                        length = (length << 8) | _data[_offset++];
                    }
                    if (length < 128)
                        throw new CryptographicException("Non-canonical RSA key length encoding.");
                }

                if (length > _data.Length - _offset)
                    throw new CryptographicException("Truncated RSA key value.");
                var result = new byte[length];
                Buffer.BlockCopy(_data, _offset, result, 0, length);
                _offset += length;
                return result;
            }

            public void RequireEnd()
            {
                if (!AtEnd)
                    throw new CryptographicException("Unexpected trailing RSA key data.");
            }
        }
    }
}
