// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class FileLinkItem : IEquatable<FileLinkItem>, IComparable<FileLinkItem>
    {
        [JsonIgnore]
        public Document SourceFile { get; }

        public string SourceUrl { get; }

        public string? SourceMonikerGroup { get; }

        public string TargetUrl { get; }

        public FileLinkItem(Document sourceFile, string sourceUrl, string? soureMonikerGroup, string targetUrl)
        {
            SourceFile = sourceFile;
            SourceUrl = sourceUrl;
            SourceMonikerGroup = soureMonikerGroup;
            TargetUrl = targetUrl;
        }

        public int CompareTo(FileLinkItem other)
        {
            var result = string.CompareOrdinal(SourceUrl, other.SourceUrl);
            if (result == 0)
                result = string.CompareOrdinal(TargetUrl, other.TargetUrl);
            if (result == 0)
                result = string.CompareOrdinal(SourceMonikerGroup, other.SourceMonikerGroup);

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
