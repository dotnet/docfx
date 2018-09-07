// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build
{
    public static class HashUtility
    {
        public static string GetMd5Hash(this string input)
        {
#pragma warning disable CA5351 //Not used for encryption
            using (var md5 = MD5.Create())
#pragma warning restore CA5351
            {
                return ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static Guid GetMd5Guid(this string input)
        {
#pragma warning disable CA5351 //Not used for encryption
            using (var md5 = MD5.Create())
#pragma warning restore CA5351
            {
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha1Hash(string input)
            => GetSha1Hash(new MemoryStream(Encoding.UTF8.GetBytes(input)));

        public static string GetSha1Hash(Stream stream)
        {
#pragma warning disable CA5350 //Not used for encryption
            using (var sha1 = new SHA1CryptoServiceProvider())
#pragma warning restore CA5350
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
