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
#pragma warning disable CA5351 // Do not use insecure cryptographic algorithm MD5.
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.

        public static string GetMd5Hash(this string input)
        {
            using (var md5 = MD5.Create())
            {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static Guid GetMd5Guid(this string input)
        {
            using (var md5 = MD5.Create())
            {
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha1Hash(string input)
            => GetSha1Hash(new MemoryStream(Encoding.UTF8.GetBytes(input)));

        public static string GetSha1Hash(Stream stream)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return ToHexString(sha1.ComputeHash(stream));
            }
        }

        private static string ToHexString(byte[] bytes)
        {
            var formatted = new StringBuilder(2 * bytes.Length);
            foreach (byte b in bytes)
            {
                formatted.AppendFormat("{0:x2}", b);
            }
            return formatted.ToString();
        }
    }
}
