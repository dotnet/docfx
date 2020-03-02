// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A docset is a collection of documents in the folder identified by `docfx.yml/docfx.json`.
    /// </summary>
    internal class Docset : IEquatable<Docset>, IComparable<Docset>
    {
        /// <summary>
        /// Gets the absolute path to folder containing `docfx.yml/docfx.json`, it is not necessarily the path to git repository.
        /// </summary>
        public string DocsetPath { get; }

        /// <summary>
        /// Gets the root repository of docset
        /// </summary>
        public Repository? Repository { get; }

        private readonly ConcurrentDictionary<string, Lazy<Repository?>> _repositories;

        public Docset(string docsetPath, Repository? repository)
        {
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Repository = repository;

            _repositories = new ConcurrentDictionary<string, Lazy<Repository?>>();
        }

        public int CompareTo(Docset other)
        {
            return string.CompareOrdinal(DocsetPath, other.DocsetPath);
        }

        public override int GetHashCode()
        {
            return PathUtility.PathComparer.GetHashCode(DocsetPath);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Docset);
        }

        public bool Equals(Docset? other)
        {
            if (other is null)
            {
                return false;
            }

            return PathUtility.PathComparer.Equals(DocsetPath, other.DocsetPath);
        }

        public static bool operator ==(Docset? obj1, Docset? obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(Docset? obj1, Docset? obj2)
        {
            return !Equals(obj1, obj2);
        }

        // todo: use repository provider instead
        public Repository? GetRepository(string filePath)
        {
            return GetRepositoryInternal(Path.Combine(DocsetPath, filePath));

            Repository? GetRepositoryInternal(string fullPath)
            {
                if (GitUtility.IsRepo(fullPath))
                {
                    if (Repository != null && string.Equals(fullPath, Repository.Path.Substring(0, Repository.Path.Length - 1), PathUtility.PathComparison))
                    {
                        return Repository;
                    }

                    return Repository.Create(fullPath, branch: null);
                }

                var parent = PathUtility.NormalizeFile(Path.GetDirectoryName(fullPath) ?? "");
                return !string.IsNullOrEmpty(parent)
                    ? _repositories.GetOrAdd(parent, k => new Lazy<Repository?>(() => GetRepositoryInternal(k))).Value
                    : null;
            }
        }
    }
}
