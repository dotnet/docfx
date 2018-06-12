// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class GitRepoInfoProvider
    {
        private static readonly ConcurrentDictionary<string, GitRepoInfo> s_cache
            = new ConcurrentDictionary<string, GitRepoInfo>();

        public static GitRepoInfo GetGitRepoInfo(Document document)
        {
            Debug.Assert(document != null);

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.Combine(document.Docset.DocsetPath, document.FilePath)));
        }

        public static GitRepoInfo GetGitRepoInfo(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        }

        private static GitRepoInfo GetFolderGitRepoInfo(string folder)
        {
            Debug.Assert(!string.IsNullOrEmpty(folder));
            Debug.Assert(Directory.Exists(folder));

            folder = PathUtility.NormalizeFolder(folder, false);
            return s_cache.GetOrAdd(folder, GetFolderGitRepoInfoCore);
        }

        private static GitRepoInfo GetFolderGitRepoInfoCore(string folder)
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
