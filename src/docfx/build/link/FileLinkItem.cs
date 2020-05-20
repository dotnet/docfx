// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class FileLinkItem : IEquatable<FileLinkItem>, IComparable<FileLinkItem>
    {
        [JsonIgnore]
        public FilePath InclusionRoot { get; }

        public string? SourceGitUrl { get; }

        public int SourceLine { get; }

        public string SourceUrl { get; }

        public string? SourceMonikerGroup { get; }

        public string TargetUrl { get; }

        public FileLinkItem(
            FilePath inclusionRoot, string sourceUrl, string? sourceMonikerGroup, string targetUrl, string? sourceGitUrl, int sourceLine)
        {
            InclusionRoot = inclusionRoot;
            SourceUrl = sourceUrl;
            SourceGitUrl = sourceGitUrl;
            SourceMonikerGroup = sourceMonikerGroup;
            TargetUrl = targetUrl;
            SourceLine = sourceLine;
        }

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
                result = SourceLine - other.SourceLine;
            }
            return result;
        }

        public bool Equals(FileLinkItem? other)
        {
            if (other is null)
            {
                return false;
            }

            return SourceUrl == other.SourceUrl
                && TargetUrl == other.TargetUrl
                && SourceMonikerGroup == other.SourceMonikerGroup
                && SourceLine == other.SourceLine;
        }

        public override bool Equals(object? obj) => Equals(obj as FileLinkItem);

        public override int GetHashCode() => HashCode.Combine(SourceUrl, TargetUrl, SourceMonikerGroup);
    }
}
