// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class GitLockProvider
    {
        public static GitLock GetGitVersion(this GitLock gitLock, string href, string branch)
        {
            Debug.Assert(gitLock != null);

            if (gitLock.Git.TryGetValue($"{href}#{branch}", out var subGitLock))
            {
                return subGitLock;
            }

            if (branch == "master" && gitLock.Git.TryGetValue($"{href}", out subGitLock))
            {
                return subGitLock;
            }

            return null;
        }

        public static GitLock Load(string docset, SourceInfo<string> gitLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(gitLockPath))
            {
                return null;
            }

            // dependency lock path can be a place holder for saving usage
            if (!UrlUtility.IsHttp(gitLockPath))
            {
                if (!File.Exists(Path.Combine(docset, gitLockPath)))
                {
                    return null;
                }
            }

            var content = RestoreFileMap.GetRestoredFileContent(docset, gitLockPath, fallbackDocset: null);
            Log.Write(content);

            return JsonUtility.Deserialize<GitLock>(content, new FilePath(gitLockPath));
        }

        public static void Save(string docset, string gitLockPath, GitLock gitLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(gitLockPath));

            var content = JsonUtility.Serialize(gitLock, indent: true);

            if (!UrlUtility.IsHttp(gitLockPath))
            {
                var path = Path.Combine(docset, gitLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ProcessUtility.WriteFile(path, content);
            }
        }
    }
}
