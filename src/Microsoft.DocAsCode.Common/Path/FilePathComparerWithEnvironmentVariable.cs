// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Common;

public class FilePathComparerWithEnvironmentVariable
    : IEqualityComparer<string>
{
    private readonly FilePathComparer _inner;

    public static readonly FilePathComparerWithEnvironmentVariable OSPlatformSensitiveComparer = new(new FilePathComparer());
    public static readonly FilePathComparerWithEnvironmentVariable OSPlatformSensitiveRelativePathComparer = new(new FilePathComparer(true));

    public FilePathComparerWithEnvironmentVariable(FilePathComparer inner)
    {
        _inner = inner;
    }

    public bool Equals(string x, string y)
    {
        return _inner.Equals(Environment.ExpandEnvironmentVariables(x), Environment.ExpandEnvironmentVariables(y));
    }

    public int GetHashCode(string obj)
    {
        return _inner.GetHashCode(Environment.ExpandEnvironmentVariables(obj));
    }
}
