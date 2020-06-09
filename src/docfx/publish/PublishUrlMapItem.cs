// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapItem : IEqualityComparer<PublishUrlMapItem>, IComparable<PublishUrlMapItem>
    {
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
        }

        public bool Equals([AllowNull] PublishUrlMapItem x, [AllowNull] PublishUrlMapItem y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }
            return PathUtility.PathComparer.Compare(x.Url, y.Url) == 0 && x.Monikers.Intersects(y.Monikers);
        }

        public int GetHashCode([DisallowNull] PublishUrlMapItem obj)
        {
            return PathUtility.PathComparer.GetHashCode(obj.Url);
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
    }
}
