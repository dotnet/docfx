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
        /// <summary>
        /// Get md5 hash string
        /// </summary>
        /// <param name="input">The input string</param>
        public static string GetMd5String(this string input)
            => GetMd5String(new MemoryStream(Encoding.UTF8.GetBytes(input)));

        /// <summary>
        /// Get md5 hash string from stream
        /// </summary>
        /// <param name="stream">The input stream</param>
        public static string GetMd5String(Stream stream)
        {
#pragma warning disable CA5351 //Not used for encryption
            using (var md5 = MD5.Create())
#pragma warning restore CA5351
            {
                return new Guid(md5.ComputeHash(stream)).ToString();
            }
        }
    }
}
