// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapItem : IEquatable<PublishUrlMapItem>, IComparable<PublishUrlMapItem>
    {
        private readonly int _hashCode;

        public string Url { get; }

        public string OutputPath { get; }

        public MonikerList Monikers { get; }

        public FilePath SourcePath { get; }

        public PublishUrlMapItem(string url, string outputPath, MonikerList monikers, FilePath sourcePath)
        {
            Url = url;
            OutputPath = outputPath;
            Monikers = monikers;
            SourcePath = sourcePath;
            _hashCode = PathUtility.PathComparer.GetHashCode(Url);
        }

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

        public override bool Equals(object? obj) => Equals(obj as PublishUrlMapItem);

        public override int GetHashCode() => _hashCode;
    }
}
