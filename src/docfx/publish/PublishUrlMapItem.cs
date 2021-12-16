// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

internal sealed record PublishUrlMapItem(string Url, string OutputPath, MonikerList Monikers, FilePath SourcePath)
    : IComparable<PublishUrlMapItem>
{
    private readonly int _hashCode = PathUtility.PathComparer.GetHashCode(Url);

    public int CompareTo(PublishUrlMapItem? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = Monikers.CompareTo(other.Monikers);
        if (result == 0)
        {
            result = SourcePath.CompareTo(other.SourcePath);
        }
        return result;
    }

    public bool Equals([AllowNull] PublishUrlMapItem other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }
        return PathUtility.PathComparer.Compare(Url, other.Url) == 0 && Monikers.Intersects(other.Monikers);
    }

    public override int GetHashCode() => _hashCode;
}
