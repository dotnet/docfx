// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class GitRepoInfoProvider
    {
        private readonly ConcurrentDictionary<string, GitRepoInfo> s_cache
            = new ConcurrentDictionary<string, GitRepoInfo>();

        public GitRepoInfo GetGitRepoInfo(Document document)
        {
            Debug.Assert(document != null);

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.Combine(document.Docset.DocsetPath, document.FilePath)));
        }

        public GitRepoInfo GetGitRepoInfo(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        }

        private GitRepoInfo GetFolderGitRepoInfo(string folder)
        {
            Debug.Assert(!string.IsNullOrEmpty(folder));
            Debug.Assert(Directory.Exists(folder));

            folder = PathUtility.NormalizeFile(folder);
            return s_cache.GetOrAdd(folder, GetFolderGitRepoInfoCore);
        }

        private GitRepoInfo GetFolderGitRepoInfoCore(string folder)
        {
            if (GitUtility.IsRepo(folder))
                return GitRepoInfo.CreateAsync(folder).Result;

            var parent = folder.Substring(0, folder.LastIndexOf("/", System.StringComparison.Ordinal));
            return Directory.Exists(parent)
                ? s_cache.GetOrAdd(parent, GetFolderGitRepoInfoCore)
                : null;
        }
    }
}
