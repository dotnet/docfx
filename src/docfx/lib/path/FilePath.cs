// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a serializable machine independent file identifier.
    /// </summary>
    internal class FilePath : IEquatable<FilePath>, IComparable<FilePath>
    {
        /// <summary>
        /// Gets the file path relative to the main docset(fallback docset).
        /// </summary>
        public PathString Path { get; }

        /// <summary>
        /// Gets the name of the dependency if it is from dependency repo.
        /// </summary>
        public PathString DependencyName { get; }

        /// <summary>
        /// Gets the value to indicate where is this file from.
        /// </summary>
        public FileOrigin Origin { get; }

        /// <summary>
        /// Gets the commit id if this file is owned by a git repository and is not the latest version.
        /// </summary>
        public string? Commit { get; }

        /// <summary>
        /// Indicate if the file is from git commit history.
        /// </summary>
        public bool IsFromHistory => Commit != null;

        public FilePath(string path, FileOrigin origin = FileOrigin.Default)
        {
            Debug.Assert(origin != FileOrigin.Dependency);

            Path = new PathString(path);
            Origin = origin;
        }

        public FilePath(string path, string? commit, FileOrigin origin)
        {
            Path = new PathString(path);
            Origin = origin;
            Commit = commit;
        }

        public FilePath(string path, PathString dependencyName)
        {
            Path = new PathString(System.IO.Path.Combine(dependencyName, path));
            DependencyName = dependencyName;
            Origin = FileOrigin.Dependency;
        }

        /// <summary>
        /// Gets the path relative to docset root or dependency docset root
        /// </summary>
        public string GetPathToOrigin()
        {
            if (Origin == FileOrigin.Dependency)
            {
                Debug.Assert(!string.IsNullOrEmpty(DependencyName));
                return PathUtility.NormalizeFile(System.IO.Path.GetRelativePath(DependencyName, Path));
            }

            return Path;
        }

        public static bool operator ==(FilePath? a, FilePath? b) => Equals(a, b);

        public static bool operator !=(FilePath? a, FilePath? b) => !Equals(a, b);

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

            return tags.Length > 0 ? $"{Path} {tags}" : $"{Path}";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path, DependencyName, Origin, Commit);
        }

        public bool Equals(FilePath? other)
        {
            if (other is null)
            {
                return false;
            }

            return Path.Equals(other.Path) &&
                   DependencyName.Equals(other.DependencyName) &&
                   other.Origin == Origin &&
                   Commit == other.Commit;
        }

        public int CompareTo(FilePath other)
        {
            var result = Path.CompareTo(other.Path);
            if (result == 0)
                result = Origin.CompareTo(other.Origin);
            if (result == 0)
                result = DependencyName.CompareTo(other.DependencyName);
            if (result == 0)
                result = string.CompareOrdinal(Commit, other.Commit);

            return result;
        }

        public bool EndsWith(string value) => Path.Value.EndsWith(value, PathUtility.PathComparison);
    }
}
