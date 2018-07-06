// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build
{
    public static class StringExtensions
    {
        /// <summary>
        /// Get md5 hash string
        /// </summary>
        /// <param name="path">The path string</param>
        public static string GetMd5String(this string path)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, Encoding.Unicode, 0x100, true))
                {
                    writer.Write(path);
                }
#pragma warning disable CA5351 // Not used for secret
                using (var md5 = MD5.Create())
#pragma warning restore CA5351
                {
                    return Convert.ToBase64String(md5.ComputeHash(ms.ToArray()));
                }
            }
        }
    }
}
