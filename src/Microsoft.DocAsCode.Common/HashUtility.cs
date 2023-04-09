// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.DocAsCode.Common;

public static class HashUtility
{
    public static byte[] GetSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    public static byte[] GetSha256Hash(string content)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
    }

    public static string GetSha256HashString(string content)
        => Convert.ToBase64String(GetSha256Hash(content));
}
