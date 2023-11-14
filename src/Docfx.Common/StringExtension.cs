// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public static class StringExtension
{
    public static string ForwardSlashCombine(this string baseAddress, string relativeAddress)
    {
        if (string.IsNullOrEmpty(baseAddress)) return relativeAddress;
        return baseAddress + "/" + relativeAddress;
    }

    public static string BackSlashToForwardSlash(this string input)
    {
        return input?.Replace('\\', '/');
    }

    public static string ToDelimitedString(this IEnumerable<string> input, string delimiter = ",")
    {
        if (input == null)
        {
            return null;
        }

        return string.Join(delimiter, input);
    }

    /// <summary>
    /// Should not convert path to lower case as under Linux/Unix, path is case sensitive
    /// Also, Website URL should be case sensitive consider the server might be running under Linux/Unix
    /// So we could even not lower the path under Windows as the generated YAML should be ideally OS irrelevant
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string ToNormalizedFullPath(this string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Path.GetFullPath(path).BackSlashToForwardSlash().TrimEnd('/');
    }

    public static string ToNormalizedPath(this string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return path.BackSlashToForwardSlash().TrimEnd('/');
    }

    public static string ToDisplayPath(this string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    public static string TrimEnd(this string input, string suffixToRemove)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(suffixToRemove))
        {
            return input;
        }

        if (input.EndsWith(suffixToRemove, StringComparison.Ordinal))
        {
            return input.Substring(0, input.LastIndexOf(suffixToRemove));
        }
        else
        {
            return input;
        }
    }
}
