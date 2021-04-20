// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class HashUtility
    {
        public static string GetMd5Hash(string input)
        {
            using var md5 = MD5.Create(); // lgtm [cs/weak-crypto]
            return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static Guid GetMd5Guid(string input)
        {
            using var md5 = MD5.Create(); // lgtm [cs/weak-crypto]
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string GetSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            return ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string GetSha256HashShort(string input)
        {
            using var sha256 = SHA256.Create();
            return ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)), 16);
        }

        public static uint GetFnv1A32Hash(ReadOnlySpan<byte> input)
        {
            var hash = 2166136261;
            for (var i = 0; i < input.Length; i++)
            {
                hash = (hash * 16777619) ^ input[i];
            }
            return hash;
        }

        private static string ToHexString(byte[] bytes, int digits = 0)
        {
            var formatted = new StringBuilder(2 * bytes.Length);
            if (digits == 0)
            {
                digits = bytes.Length;
            }

            for (var i = 0; i < digits; i++)
            {
                formatted.AppendFormat("{0:x2}", bytes[i]);
            }
            return formatted.ToString();
        }
    }
}
