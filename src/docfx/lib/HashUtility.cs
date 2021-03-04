// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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

        public static string GetMd5HashShort(string input)
        {
            using var md5 = MD5.Create(); // lgtm [cs/weak-crypto]
            return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)), 4);
        }

        public static Guid GetMd5Guid(string input)
        {
            using var md5 = MD5.Create(); // lgtm [cs/weak-crypto]
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string GetSha1Hash(string input)
        {
            using var sha1 = new SHA1CryptoServiceProvider(); // lgtm [cs/weak-crypto]
            return ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string GetSha1Hash(Stream input)
        {
            using var sha1 = SHA1.Create(); // lgtm [cs/weak-crypto]
            return ToHexString(sha1.ComputeHash(input));
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

        public static ulong GetFnv1A64Hash(ReadOnlySpan<byte> input)
        {
            var hash = 14695981039346656037;
            for (var i = 0; i < input.Length; i++)
            {
                hash = (hash * 1099511628211) ^ input[i];
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
