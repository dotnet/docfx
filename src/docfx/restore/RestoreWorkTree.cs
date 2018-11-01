// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreWorkTree
    {
        public const int MaxKeepingDays = 10;

        public static string GetRestoreWorkTreeDir(string restoreDir, string workTreeHead)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        public static async Task<List<(string href, string head)>> AddWorkTrees(string restoreDir, List<string> hrefs, Config config)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));
            Debug.Assert(hrefs != null && hrefs.Any());

            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            var (url, _) = GitUtility.GetGitRemoteInfo(hrefs.First());
            var workTreeHeads = new ConcurrentBag<(string href, string head)>();

            await ProcessUtility.RunInsideMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    try
                    {
                        await FetchOrCloneRepo();
                        await AddWorkTrees();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw Errors.GitCloneFailed(hrefs.First()).ToException(ex);
                    }
                });

            Task FetchOrCloneRepo()
            {
                var gitConfigs =
                       from http in config.Http
                       where url.StartsWith(http.Key)
                       from header in http.Value.Headers
                       select $"-c http.{http.Key}.extraheader=\"{header.Key}: {header.Value}\"";

                var gitConfig = string.Join(' ', gitConfigs);

                if (GitUtility.IsRepo(restoreDir))
                {
                    // already exists, just pull the new updates from remote
                    // fetch bare repo: https://stackoverflow.com/questions/3382679/how-do-i-update-my-bare-repo
                    return GitUtility.Fetch(restorePath, url, "+refs/heads/*:refs/heads/*", gitConfig);
                }
                else
                {
                    // doesn't exist yet, clone this repo to a specified branch
                    return GitUtility.Clone(restoreDir, url, restorePath, gitConfig: gitConfig, bare: true);
                }
            }

            async Task AddWorkTrees()
            {
                var existingWorkTrees = new ConcurrentDictionary<string, int>((await GitUtility.ListWorkTrees(restorePath, false)).ToDictionary(k => k, v => 0));
                await ParallelUtility.ForEach(hrefs, async href =>
                {
                    var (_, rev) = GitUtility.GetGitRemoteInfo(href);
                    var workTreeHead = $"{await GitUtility.Revision(restorePath, rev)}-{rev}";
                    var workTreePath = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    if (existingWorkTrees.TryAdd(workTreePath, 0))
                    {
                        // use branch name instead of commit hash
                        // https://git-scm.com/docs/git-worktree#_commands
                        await GitUtility.AddWorkTree(restorePath, rev, workTreePath);
                    }

                    workTreeHeads.Add((href, workTreeHead));
                });
            }

            Debug.Assert(hrefs.Count == workTreeHeads.Count);
            return workTreeHeads.ToList();
        }

        // clean up un-used work trees
        public static async Task<List<string>> CleanupWorkTrees(string restoreDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));

            var remainingWorkTrees = new List<string>();

            if (!GitUtility.IsRepo(restoreDir))
            {
                return remainingWorkTrees;
            }

            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));

            await ProcessUtility.RunInsideMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    var existingWorkTreeFolders = Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                                               .Select(f => PathUtility.NormalizeFolder(f)).Where(f => !f.EndsWith(".git/")).ToList();

                    foreach (var existingWorkTreeFolder in existingWorkTreeFolders)
                    {
                        if (new DirectoryInfo(existingWorkTreeFolder).LastAccessTimeUtc + TimeSpan.FromDays(MaxKeepingDays) < DateTime.UtcNow)
                        {
                            Directory.Delete(existingWorkTreeFolder, true);
                        }
                    }

                    await GitUtility.PruneWorkTrees(restorePath);
                    remainingWorkTrees = await GitUtility.ListWorkTrees(restorePath, false);
                });

            return remainingWorkTrees;
        }
    }
}
