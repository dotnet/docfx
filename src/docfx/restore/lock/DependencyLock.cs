// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class DependencyLock : DependencyVersion
    {
        public Dictionary<string, DependencyLock> Git { get; set; } = new Dictionary<string, DependencyLock>();

        public Dictionary<string, DependencyVersion> Downloads { get; set; } = new Dictionary<string, DependencyVersion>();

        public DependencyLock GetGitLock(string href, string branch)
        {
            if (Git.TryGetValue($"{href}#{branch}", out var dependencyLock))
            {
                return dependencyLock;
            }

            if (branch == "master" && Git.TryGetValue($"{href}", out dependencyLock))
            {
                return dependencyLock;
            }

            return null;
        }

        public bool ContainsGitLock(string href)
        {
            return Git.ContainsKey(href) || Git.Keys.Any(g => g.StartsWith($"{href}#"));
        }

        public static async Task<DependencyLock> Load(string docset, string dependencyLockPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            if (string.IsNullOrEmpty(dependencyLockPath))
            {
                return null;
            }

            var (_, restoredLockFile) = RestoreMap.GetFileRestorePath(docset, dependencyLockPath);

            // todo: add process lock
            return JsonUtility.Deserialize<DependencyLock>(await ProcessUtility.ReadFile(restoredLockFile));
        }

        public static Task<DependencyLock> Load(string docset, CommandLineOptions commandLineOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(docset));

            var (errors, config) = ConfigLoader.TryLoad(docset, commandLineOptions);

            return Load(docset, config.DependencyLock);
        }
    }
}
