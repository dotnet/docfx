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
            => GetMd5Hash(new MemoryStream(Encoding.UTF8.GetBytes(input)));

        public static string GetMd5Hash(Stream stream)
        {
#pragma warning disable CA5351 //Not used for encryption
            using (var md5 = MD5.Create())
#pragma warning restore CA5351
            {
                return new Guid(md5.ComputeHash(stream)).ToString();
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
                var hash = sha1.ComputeHash(stream);
                var formatted = new StringBuilder(2 * hash.Length);
                foreach (byte b in hash)
                {
                    formatted.AppendFormat("{0:x2}", b);
                }

                return formatted.ToString();
            }
        }
    }
}
