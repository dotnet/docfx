// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class DependencyItem
{
    public FilePath From { get; }

    [JsonIgnore]
    public bool Transitive { get; }

    public FilePath To { get; }

    public DependencyType Type { get; }

    public DependencyItem(FilePath from, FilePath to, DependencyType type, bool transitive)
    {
        From = from;
        Transitive = transitive;
        To = to;
        Type = type;
    }

    public override int GetHashCode() => HashCode.Combine(To, From, Type);

    public override bool Equals(object? obj) => Equals(obj as DependencyItem);

    public bool Equals(DependencyItem? other)
    {
        if (other is null)
        {
            return false;
        }

        return From.Equals(other.From) && To.Equals(other.To) && Type == other.Type;
    }
}
