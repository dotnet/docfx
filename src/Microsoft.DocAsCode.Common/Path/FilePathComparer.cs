// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common;

public class FilePathComparer
    : IEqualityComparer<string>
{
    private readonly bool _ignoreToFullPath;
    private readonly static StringComparer _stringComparer = GetStringComparer();

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
