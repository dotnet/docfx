// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a serializable machine independent file identifier.
    /// </summary>
    internal record FilePath : IComparable<FilePath>
    {
        private readonly int _hashCode;

        /// <summary>
        /// Gets the file path relative to the main docset.
        /// </summary>
        public PathString Path { get; init; }

        /// <summary>
        /// Gets the file format.
        /// </summary>
        public FileFormat Format { get; }

        /// <summary>
        /// Gets the name of the dependency if it is from dependency repo.
        /// </summary>
        public PathString DependencyName { get; }

        /// <summary>
        /// Gets the value to indicate where is this file from.
        /// </summary>
        public FileOrigin Origin { get; }

        /// <summary>
        /// Indicate if the file is from git commit history.
        /// </summary>
        public bool IsGitCommit { get; }

        /// <summary>
        /// Monikers for redirection files.
        /// </summary>
        public MonikerList RedirectionMonikers { get; }

        /// <summary>
        /// Creates an unknown file path.
        /// </summary>
        public FilePath(string path)
        {
            Path = new PathString(path);
            Format = GetFormat(path);
            Origin = FileOrigin.External;

            _hashCode = HashCode.Combine(Path, DependencyName, Origin, IsGitCommit);
        }

        private FilePath(FileOrigin origin, PathString path, PathString dependencyName, bool isGitCommit, MonikerList monikers)
        {
            Path = path;
            Origin = origin;
            DependencyName = dependencyName;
            IsGitCommit = isGitCommit;
            Format = GetFormat(path);
            RedirectionMonikers = monikers;

            _hashCode = HashCode.Combine(Path, DependencyName, Origin, IsGitCommit, RedirectionMonikers);
        }

        public static FilePath Content(PathString path)
        {
            Debug.Assert(!System.IO.Path.IsPathRooted(path));
            return new FilePath(FileOrigin.Main, path, default, default, default);
        }

        public static FilePath Redirection(PathString path, MonikerList redirectionMonikers)
        {
            Debug.Assert(!System.IO.Path.IsPathRooted(path));
            return new FilePath(FileOrigin.Redirection, path, default, default, redirectionMonikers);
        }

        public static FilePath Fallback(PathString path, bool isGitCommit = false)
        {
            Debug.Assert(!System.IO.Path.IsPathRooted(path));
            return new FilePath(FileOrigin.Fallback, path, default, isGitCommit, default);
        }

        public static FilePath Dependency(PathString path, PathString dependencyName)
        {
            Debug.Assert(!System.IO.Path.IsPathRooted(path));
            Debug.Assert(path.StartsWithPath(dependencyName, out _));
            return new FilePath(FileOrigin.Dependency, path, dependencyName, default, default);
        }

        public static FilePath Generated(PathString path)
        {
            Debug.Assert(!System.IO.Path.IsPathRooted(path));
            return new FilePath(FileOrigin.Generated, path, default, default, default);
        }

        public override string ToString()
        {
            var tags = "";

            switch (Origin)
            {
                case FileOrigin.Main:
                case FileOrigin.External:
                    break;

                case FileOrigin.Dependency:
                    tags += $"[{DependencyName}]";
                    break;

                default:
                    tags += $"[{Origin.ToString().ToLowerInvariant()}]";
                    break;
            }

            if (RedirectionMonikers.HasMonikers)
            {
                tags += $"[{RedirectionMonikers}]";
            }

            if (IsGitCommit)
            {
                tags += $"!";
            }

            return tags.Length > 0 ? $"{Path} {tags}" : $"{Path}";
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public int CompareTo(FilePath? other)
        {
            if (other is null)
            {
                return 1;
            }

            var result = Path.CompareTo(other.Path);
            if (result == 0)
            {
                result = Origin.CompareTo(other.Origin);
            }

            if (result == 0)
            {
                result = DependencyName.CompareTo(other.DependencyName);
            }

            if (result == 0)
            {
                result = IsGitCommit.CompareTo(other.IsGitCommit);
            }

            if (result == 0)
            {
                result = RedirectionMonikers.CompareTo(other.RedirectionMonikers);
            }

            return result;
        }

        private static FileFormat GetFormat(string path)
        {
            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return FileFormat.Markdown;
            }

            if (path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                return FileFormat.Yaml;
            }

            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return FileFormat.Json;
            }

            return FileFormat.Unknown;
        }
    }
}