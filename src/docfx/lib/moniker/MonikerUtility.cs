// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal class MonikerUtility
    {
        public static string GetMonikersHash(List<string> monikers)
        {
            if (monikers.Count == 0)
            {
                return string.Empty;
            }
            return HashUtility.GetMd5Hash(string.Join(',', monikers)).Substring(0, 8);
        }
    }
}
