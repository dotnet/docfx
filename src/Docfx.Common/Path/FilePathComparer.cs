// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class FilePathComparer
    : IEqualityComparer<string>
{
    private readonly bool _ignoreToFullPath;
    private static readonly StringComparer _stringComparer = GetStringComparer();

    public static readonly FilePathComparer OSPlatformSensitiveComparer = new();
    public static readonly FilePathComparer OSPlatformSensitiveRelativePathComparer = new(true);
    public static readonly StringComparer OSPlatformSensitiveStringComparer = GetStringComparer();

    public FilePathComparer()
        : this(false) { }

    public FilePathComparer(bool ignoreToFullPath)
    {
        _ignoreToFullPath = ignoreToFullPath;
    }

    public bool Equals(string x, string y)
    {
        if (_ignoreToFullPath)
        {
            return _stringComparer.Equals(x.ToNormalizedPath(), y.ToNormalizedPath());
        }
        else
        {
            return _stringComparer.Equals(x.ToNormalizedFullPath(), y.ToNormalizedFullPath());
        }
    }

    public int GetHashCode(string obj)
    {
        string path;
        if (_ignoreToFullPath)
        {
            path = obj.ToNormalizedPath();
        }
        else
        {
            path = obj.ToNormalizedFullPath();
        }

        return _stringComparer.GetHashCode(path);
    }

    private static StringComparer GetStringComparer()
    {
        if (PathUtility.IsPathCaseInsensitive())
        {
            return StringComparer.OrdinalIgnoreCase;
        }
        return StringComparer.Ordinal;
    }
}
