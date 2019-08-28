// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a serializable machine independent file identifier.
    /// </summary>
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
        /// Gets the value to indicate where is this file from.
        /// </summary>
        public FileOrigin Origin { get; }

        /// <summary>
        /// Gets the commit id if this file is owned by a git repository and it not the latest version.
        /// </summary>
        public string Commit { get; }

        public FilePath(string path, FileOrigin origin = FileOrigin.Default)
        {
            Debug.Assert(origin != FileOrigin.Dependency);

            Path = PathUtility.NormalizeFile(path);
            Origin = origin;
        }

        public FilePath(string path, string commit, FileOrigin origin)
        {
            Path = PathUtility.NormalizeFile(path);
            Origin = origin;
            Commit = commit;
        }

        public FilePath(string path, string dependencyName)
        {
            Path = PathUtility.NormalizeFile(path);
            DependencyName = dependencyName;
            Origin = FileOrigin.Dependency;
        }

        public static bool operator ==(FilePath a, FilePath b) => Equals(a, b);

        public static bool operator !=(FilePath a, FilePath b) => !Equals(a, b);

        public override string ToString()
        {
            var tags = "";

            switch (Origin)
            {
                case FileOrigin.Default:
                    break;

                case FileOrigin.Dependency:
                    tags += $"[{DependencyName}]";
                    break;

                default:
                    tags += $"[{Origin.ToString().ToLowerInvariant()}]";
                    break;
            }

            if (Commit != null)
            {
                tags += $"[{Commit}]";
            }

            return tags.Length > 0 ? $"{Path} {tags}" : Path;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PathUtility.PathComparer.GetHashCode(Path), Origin, DependencyName, Commit);
        }

        public bool Equals(FilePath other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Path, other.Path, PathUtility.PathComparison) &&
                   other.Origin == Origin &&
                   DependencyName == other.DependencyName &&
                   Commit == other.Commit;
        }

        public int CompareTo(FilePath other)
        {
            var result = string.Compare(Path, other.Path, PathUtility.PathComparison);
            if (result == 0)
                result = Origin.CompareTo(other.Origin);
            if (result == 0)
                result = DependencyName.CompareTo(other.DependencyName);
            if (result == 0)
                result = Commit.CompareTo(other.Commit);

            return result;
        }

        public bool EndsWith(string value, StringComparison stringComparison)
            => Path.EndsWith(value, stringComparison);
    }
}
