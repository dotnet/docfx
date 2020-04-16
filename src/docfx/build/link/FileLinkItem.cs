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

        [JsonIgnore]
        public FilePath ReferencingFile { get; }

        public string SourceGitUrl { get; set; } = "";

        public int SourceLine { get; }

        public string SourceUrl { get; }

        public string? SourceMonikerGroup { get; }

        public string TargetUrl { get; }

        public FileLinkItem(FilePath inclusionRoot, FilePath referencingFile, string sourceUrl, string? sourceMonikerGroup, string targetUrl, int sourceLine)
        {
            InclusionRoot = inclusionRoot;
            ReferencingFile = referencingFile;
            SourceUrl = sourceUrl;
            SourceMonikerGroup = sourceMonikerGroup;
            TargetUrl = targetUrl;
            SourceLine = sourceLine;
        }

        public int CompareTo(FileLinkItem other)
        {
            var result = string.CompareOrdinal(SourceUrl, other.SourceUrl);
            if (result == 0)
            {
                result = string.CompareOrdinal(TargetUrl, other.TargetUrl);
            }
            if (result == 0)
            {
                result = string.CompareOrdinal(SourceMonikerGroup, other.SourceMonikerGroup);
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
                && SourceMonikerGroup == other.SourceMonikerGroup;
        }

        public override bool Equals(object? obj) => Equals(obj as FileLinkItem);

        public override int GetHashCode() => HashCode.Combine(SourceUrl, TargetUrl, SourceMonikerGroup);
    }
}
