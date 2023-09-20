// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Docfx.Common;

public static class HashUtility
{
    public static byte[] GetSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(stream);
    }

    public static byte[] GetSha256Hash(string content)
        => SHA256.HashData(Encoding.UTF8.GetBytes(content));

    public static string GetSha256HashString(string content)
        => Convert.ToBase64String(GetSha256Hash(content));
}
