// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Docs.Build;

internal static class HashUtility
{
    public static string GetSha256Hash(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    public static string GetSha256HashShort(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)), 0, 16).ToLowerInvariant();
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
}
