// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class RepositoryUtility
    {
        private static readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        public static (Repository repo, string pathToRepo) GetRepository(Document document)
        {
            var fullPath = PathUtility.NormalizeFile(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var repo = GetRepository(fullPath);
            if (repo == null)
                return default;

            return (repo, PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath)));
        }

        public static Repository GetRepository(string fullPath)
        {
            if (GitUtility.IsRepo(fullPath))
                return Repository.Create(fullPath);

            var parent = Path.GetDirectoryName(fullPath);
            return !string.IsNullOrEmpty(parent)
                ? _repositoryByFolder.GetOrAdd(parent, GetRepository)
                : null;
        }
    }
}
