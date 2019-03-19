// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class HashUtility
    {
#pragma warning disable CA5351 // Do not use insecure cryptographic algorithm MD5.
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.

        public static string GetMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetMd5HashShort(string input)
        {
            using (var md5 = MD5.Create())
            {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)), 4);
            }
        }

        public static Guid GetMd5Guid(string input)
        {
            using (var md5 = MD5.Create())
            {
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha1Hash(string input)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha1Hash(Stream input)
        {
            using (var sha1 = SHA1.Create())
            {
                return ToHexString(sha1.ComputeHash(input));
            }
        }

        public static string GetFileSha1Hash(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return ToHexString(sha1.ComputeHash(stream));
            }
        }

        public static string GetMd5HashShort(List<string> list, string separator = ",")
        {
            if (list is null || list.Count == 0)
            {
                return null;
            }

            return GetMd5HashShort(string.Join(separator, list));
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
