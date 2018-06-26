// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        private static readonly string s_restoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "git");

        public static Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            using (ConsoleLog.Measure("Restore dependencies"))
            {
                // Restore has to use Config directly, it cannot depend on Docset,
                // because Docset assumes the repo to physically exist on disk.
                var config = Config.Load(docsetPath, options);

                report.Configure(docsetPath, config);

                var restoredDirs = new HashSet<string>();

                return ParallelUtility.ForEach(
                    config.Dependencies.Values,
                    (href, restoreChild) => RestoreDependentRepo(href, options, restoreChild, restoredDirs),
                    progress: ConsoleLog.Progress);
            }
        }

        /// <summary>
        /// Get git repo information from git remote href, like https://github.com/org/repo#master
        /// </summary>
        /// <param name="remote">The git remote href</param>
        /// <returns>The git repo information including local dir, git remote url and the ref sepc</returns>
        public static (string restoreDir, string url, string refSpec) GetGitRestoreInfo(string remote)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var (path, _, fragment) = HrefUtility.SplitHref(remote);

            var refSpec = (string.IsNullOrEmpty(fragment) || fragment.Length <= 1) ? "master" : fragment.Substring(1);
            var uri = new Uri(path);
            var url = uri.GetLeftPart(UriPartial.Path);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(s_restoreDir, repo, PathUtility.Encode(refSpec));

            return (PathUtility.NormalizeFolder(dir), url, refSpec);
        }

        // Recursively restore dependent repo including their children
        private static async Task RestoreDependentRepo(string href, CommandLineOptions options, Action<string> restoreChild, HashSet<string> restoredDirs)
        {
            var childDir = await FetchOrCloneDependentRepo(href);

            if (restoredDirs.Add(childDir) && Config.LoadIfExists(childDir, options, out var childConfig))
            {
                foreach (var (key, childHref) in childConfig.Dependencies)
                {
                    restoreChild(childHref);
                }
            }
        }

        // Fetch or clone dependent repo to local
        private static async Task<string> FetchOrCloneDependentRepo(string href)
        {
            var (restoreDir, url, rev) = GetGitRestoreInfo(href);

            var lockRelativePath = Path.Combine(Path.GetRelativePath(s_restoreDir, restoreDir), ".lock");
            await ProcessUtility.ProcessLock(
                lockRelativePath,
                async () =>
                {
                    if (GitUtility.IsRepo(restoreDir))
                    {
                        // already exists, just pull the new updates from remote
                        await GitUtility.Pull(restoreDir);
                    }
                    else
                    {
                        // doesn't exist yet, clone this repo to a specified branch
                        await GitUtility.Clone(restoreDir, url, restoreDir, rev);
                    }
                });

            return restoreDir;
        }
    }
}
