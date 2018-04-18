// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        private static readonly string s_restoreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "git");

        public static Task Run(string docsetPath, CommandLineOptions options, Context context)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            var config = Config.Load(docsetPath, options);

            return ParallelUtility.ForEach(config.Dependencies.Values, (href, restoreChild) => RestoreDependentRepo(href, options, restoreChild));
        }

        /// <summary>
        /// Get git repo information from git remote href, like https://github.com/org/repo#master
        /// </summary>
        /// <param name="remote">The git remote href</param>
        /// <returns>The git repo information including local dir, git remote url and branch</returns>
        public static (string dir, string url, string rev) GetGitInfo(string remote)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var uri = new Uri(remote);
            var rev = (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment.Length <= 1) ? "master" : uri.Fragment.Substring(1);
            var url = uri.GetLeftPart(UriPartial.Path);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(s_restoreDir, repo);

            return (PathUtility.NormalizeFolder(dir), url, rev);
        }

        // Recursively restore dependent repo including their children
        private static async Task RestoreDependentRepo(string href, CommandLineOptions options, Action<string> restoreChild)
        {
            var childDir = await FetchOrCloneDependentRepo(href);

            if (Config.TryLoad(childDir, options, out var childConfig))
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
            var (dir, url, rev) = GetGitInfo(href);

            await ProcessUtility.ProcessLock(
                async () =>
                {
                    if (GitUtility.IsRepo(dir))
                    {
                        // already exists, just pull the new updates from remote
                        await GitUtility.Pull(dir);
                    }
                    else
                    {
                        // doesn't exist yet, clone this repo
                        await GitUtility.Clone(dir, url, dir);

                        if (!string.IsNullOrEmpty(rev))
                        {
                            await GitUtility.Checkout(dir, false, rev);
                        }
                    }
                },
                Path.Combine(dir, ".lock"));

            return dir;
        }
    }
}
