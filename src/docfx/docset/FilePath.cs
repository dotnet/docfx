// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class FilePath : IEquatable<FilePath>, IComparable<FilePath>
    {
        /// <summary>
        /// The file relative path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the value to indicate where is this file from
        /// </summary>
        public FileOrigin Origin { get; }

        public FilePath(string path, Docset docset)
            : this(path, docset != null && docset.IsFallback() ? FileOrigin.Fallback : FileOrigin.Current)
        {
        }

        public FilePath(string path, FileOrigin from = FileOrigin.Current)
        {
            Path = path;
            Origin = from;
        }

        public static bool operator ==(FilePath a, FilePath b)
            => Equals(a, b);

        public static bool operator !=(FilePath a, FilePath b)
           => !(a == b);

        public override string ToString() => Path;

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PathUtility.PathComparer.GetHashCode(Path), Origin);
        }

        public bool Equals(FilePath other)
        {
            return other?.Path == Path && other?.Origin == Origin;
        }

        public int CompareTo(FilePath other)
        {
            var result = PathUtility.PathComparer.Compare(Path, other?.Path);
            if (result == 0)
                result = Origin.CompareTo(other?.Origin);

            return result;
        }
    }
}
