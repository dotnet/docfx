// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Glob;

    public sealed class FileMetadataItem : IEquatable<FileMetadataItem>
    {
        private static readonly StringComparer Comparer = FilePathComparer.OSPlatformSensitiveStringComparer;

        public GlobMatcher Glob { get; }
        public object Value { get; }
        public string Key { get; }

        public FileMetadataItem(GlobMatcher glob, string key, object value)
        {
            Glob = glob;
            Key = key;
            Value = value;
        }

        #region Compare & Equatable

        public bool Equals(FileMetadataItem other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Glob.Equals(other.Glob) && Comparer.Equals(Key, other.Key) && Comparer.Equals(Value.ToJsonString(), other.Value.ToJsonString());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileMetadataItem);
        }

        public override int GetHashCode()
        {
            return (Glob?.GetHashCode() ?? 0) ^ (Comparer.GetHashCode(Key) >> 1) ^ (Comparer.GetHashCode(Value.ToJsonString()) >> 2);
        }

        #endregion
    }
}
