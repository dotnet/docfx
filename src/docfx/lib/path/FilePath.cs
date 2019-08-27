// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class FilePath : IEquatable<FilePath>, IComparable<FilePath>
    {
        /// <summary>
        /// Gets the file path relative to the main docset.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the name of the dependency if it is from dependency repo.
        /// </summary>
        public string DependencyName { get; }

        /// <summary>
        /// Gets the value to indicate where is this file from
        /// </summary>
        public FileOrigin Origin { get; }

        public FilePath(string path, FileOrigin from = FileOrigin.Default)
        {
            Debug.Assert(from != FileOrigin.Dependency);

            Path = PathUtility.NormalizeFile(path);
            Origin = from;
        }

        public FilePath(string path, string dependencyName)
        {
            Path = PathUtility.NormalizeFile(path);
            DependencyName = dependencyName;
            Origin = FileOrigin.Dependency;
        }

        public static bool operator ==(FilePath a, FilePath b) => Equals(a, b);

        public static bool operator !=(FilePath a, FilePath b) => !Equals(a, b);

        public override string ToString() => Path;

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PathUtility.PathComparer.GetHashCode(Path), Origin, DependencyName);
        }

        public bool Equals(FilePath other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Path, other.Path, PathUtility.PathComparison) &&
                   other.Origin == Origin &&
                   DependencyName == other.DependencyName;
        }

        public int CompareTo(FilePath other)
        {
            var result = string.Compare(Path, other.Path, PathUtility.PathComparison);
            if (result == 0)
                result = Origin.CompareTo(other.Origin);
            if (result == 0)
                result = DependencyName.CompareTo(other.DependencyName);

            return result;
        }

        public bool EndsWith(string value, StringComparison stringComparison)
            => Path.EndsWith(value, stringComparison);
    }
}
