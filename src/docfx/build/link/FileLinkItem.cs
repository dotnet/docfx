// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal record FileLinkItem : IComparable<FileLinkItem>
{
    [JsonIgnore]
    public FilePath? InclusionRoot { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceMonikerGroup { get; init; }

    public string? TargetUrl { get; init; }

    public string? SourceGitUrl { get; init; }

    public int SourceLine { get; init; }

    public int CompareTo(FileLinkItem? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = string.CompareOrdinal(SourceUrl, other.SourceUrl);
        if (result == 0)
        {
            result = string.CompareOrdinal(TargetUrl, other.TargetUrl);
        }
        if (result == 0)
        {
            result = string.CompareOrdinal(SourceMonikerGroup, other.SourceMonikerGroup);
        }
        if (result == 0)
        {
            result = string.CompareOrdinal(SourceGitUrl, other.SourceGitUrl);
        }
        if (result == 0)
        {
            result = SourceLine - other.SourceLine;
        }
        return result;
    }
}
