// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal struct FileLinkItem : IEquatable<FileLinkItem>, IComparable<FileLinkItem>
    {
        public string SourceUrl { get; set; }

        public string SourceMonikerGroup { get; set; }

        public string TargetUrl { get; set; }

        public int CompareTo(FileLinkItem other)
        {
            var result = string.CompareOrdinal(SourceUrl, other.SourceUrl);
            if (result == 0)
                result = string.CompareOrdinal(TargetUrl, other.TargetUrl);
            if (result == 0)
                result = string.CompareOrdinal(SourceMonikerGroup, other.SourceMonikerGroup);

            return result;
        }

        public bool Equals(FileLinkItem other)
        {
            return SourceUrl == other.SourceUrl
                && TargetUrl == other.TargetUrl
                && SourceMonikerGroup == other.SourceMonikerGroup;
        }

        public override bool Equals(object obj)
        {
            return obj is FileLinkItem && Equals((FileLinkItem)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SourceUrl, TargetUrl, SourceMonikerGroup);
        }
    }
}
