// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class DependencyLock
    {
        public static DependencyLockModel GetGitLock(this DependencyLockModel dependencyLock, string href, string branch)
        {
            Debug.Assert(dependencyLock != null);

            if (dependencyLock.Git.TryGetValue($"{href}#{branch}", out var gitLock))
            {
                return gitLock;
            }

            if (branch == "master" && dependencyLock.Git.TryGetValue($"{href}", out gitLock))
            {
                return gitLock;
            }

            return null;
        }

        public static bool ContainsGitLock(this DependencyLockModel dependencyLock, string href)
        {
            Debug.Assert(dependencyLock != null);

            return dependencyLock.Git.ContainsKey(href) || dependencyLock.Git.Keys.Any(g => g.StartsWith($"{href}#"));
        }

        public static async Task<DependencyLockModel> Load(string docset, string dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            // dependency lock path can be a place holder for saving usage
            if (!HrefUtility.IsHttpHref(dependencyLockPath))
            {
                if (!File.Exists(Path.Combine(docset, dependencyLockPath)))
                {
                    return null;
                }
            }

            var (_, content, _) = await RestoreMap.GetRestoredFileContent(docset, dependencyLockPath);

            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            return JsonUtility.DeserializeData<DependencyLockModel>(content);
        }

        public static async Task Save(string docset, string dependencyLockPath, DependencyLockModel dependencyLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));
            Debug.Assert(!string.IsNullOrEmpty(dependencyLockPath));

            var content = JsonUtility.Serialize(dependencyLock, indent: true);

            if (!HrefUtility.IsHttpHref(dependencyLockPath))
            {
                var path = Path.Combine(docset, dependencyLockPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                await ProcessUtility.WriteFile(path, content);
            }

            // todo: upload to remote file directly
        }
    }
}
