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
    internal static class RestoreGit
    {
        private const int MaxWorkTreeCount = 5;

        public static string GetRestoreWorkTreeDir(string restoreDir, string workTreeHead)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        public static string GetRestoreRootDir(string url)
            => Docs.Build.Restore.GetRestoreRootDir(url, AppData.GitRestoreDir);

        public static async Task<IEnumerable<(string href, string workTreeHead)>> Restore(Config config, Func<string, Task> restoreChild, string token)
        {
            var workTreeMappings = new ConcurrentBag<(string href, string workTreeHead)>();
            var restoreItems = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => (g.Key, g.Distinct().ToList()));

            // process git restore items
            await ParallelUtility.ForEach(
               restoreItems,
               async restoreItem =>
               {
                   var (restoreDir, hrefs) = restoreItem;
                   var workTreeHeads = await RestoreDependentRepo(restoreDir, hrefs);
                   foreach (var workTreeHead in workTreeHeads)
                   {
                       workTreeMappings.Add(workTreeHead);
                   }
               },
               progress: Progress.Update);

            return workTreeMappings;

            async Task<List<(string href, string head)>> RestoreDependentRepo(string restoreDir, List<string> hrefs)
            {
                var workTreeHeads = await AddWorkTrees(restoreDir, hrefs, token);

                foreach (var (_, workTreeHead) in workTreeHeads)
                {
                    var childDir = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    await restoreChild(childDir);
                }

                return workTreeHeads;
            }
        }

        public static async Task GC(Config config, Func<string, Task> gcChild)
        {
            var restoreDirs = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => g.Key);

            await ParallelUtility.ForEach(
               restoreDirs,
               async restoreDir =>
               {
                   var leftWorkTrees = await CleanupWorkTrees(restoreDir);
                   foreach (var leftWorkTree in leftWorkTrees)
                   {
                       await gcChild(leftWorkTree);
                   }
               },
               progress: Progress.Update);
        }

        // Restore dependent repo to local and create work tree
        private static async Task<List<(string href, string head)>> AddWorkTrees(string restoreDir, List<string> hrefs, string token)
        {
            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            var (url, _) = GitUtility.GetGitRemoteInfo(hrefs.First());
            var workTreeHeads = new ConcurrentBag<(string href, string head)>();

            await ProcessUtility.CreateFileMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    await FetchOrCloneRepo();

                    await AddWorkTrees();
                });

            async Task FetchOrCloneRepo()
            {
                if (GitUtility.IsRepo(restoreDir))
                {
                    // already exists, just pull the new updates from remote
                    await GitUtility.Fetch(restorePath, GitUtility.EmbedToken(url, token));
                }
                else
                {
                    // doesn't exist yet, clone this repo to a specified branch
                    await GitUtility.WithToken(restorePath, url, token, async urlWithToken =>
                    {
                        await GitUtility.Clone(restoreDir, urlWithToken, restorePath, null, true);
                    });
                }
            }

            async Task AddWorkTrees()
            {
                var existingWorkTrees = new ConcurrentDictionary<string, int>((await GitUtility.ListWorkTrees(restorePath, false)).ToDictionary(k => k, v => 0));
                await ParallelUtility.ForEach(hrefs, async href =>
                {
                    var (_, rev) = GitUtility.GetGitRemoteInfo(href);
                    var workTreeHead = await GitUtility.Revision(restorePath, rev);
                    var workTreePath = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    if (existingWorkTrees.TryAdd(workTreePath, 0))
                    {
                        await GitUtility.AddWorkTree(restorePath, workTreeHead, workTreePath);
                    }

                    workTreeHeads.Add((href, workTreeHead));
                });
            }

            Debug.Assert(hrefs.Count == workTreeHeads.Count);
            return workTreeHeads.ToList();
        }

        // clean up un-used work trees
        private static async Task<List<string>> CleanupWorkTrees(string restoreDir)
        {
            var remainingWorkTrees = new List<string>();

            if (!GitUtility.IsRepo(restoreDir))
            {
                return remainingWorkTrees;
            }

            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));

            await ProcessUtility.CreateFileMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    var existingWorkTrees = await GitUtility.ListWorkTrees(restorePath, false);
                    if (NeedCleanupWorkTrees(existingWorkTrees.Count))
                    {
                        remainingWorkTrees = await CleanupWorkTrees(existingWorkTrees);
                    }
                });

            return remainingWorkTrees;

            bool NeedCleanupWorkTrees(int existingWorkTreeCount) => existingWorkTreeCount > MaxWorkTreeCount;

            async Task<List<string>> CleanupWorkTrees(List<string> existingWorkTrees)
            {
                var allWorkTreesInUse = await GetAllWorkTreePaths(restoreDir);
                var leftWorkTrees = new List<string>();
                foreach (var workTree in existingWorkTrees)
                {
                    if (!allWorkTreesInUse.Contains(workTree))
                    {
                        // remove not used work tree
                        Directory.Delete(workTree, true);
                    }
                    else
                    {
                        leftWorkTrees.Add(workTree);
                    }
                }
                await GitUtility.PruneWorkTrees(restorePath);

                return leftWorkTrees;
            }
        }

        private static async Task<HashSet<string>> GetAllWorkTreePaths(string restoreDir)
        {
            var allLocks = await RestoreLocker.LoadAll();
            var workTreePaths = new HashSet<string>();

            foreach (var restoreLock in allLocks)
            {
                foreach (var (href, workTreeHead) in restoreLock.Git)
                {
                    var rootDir = GetRestoreRootDir(href);
                    if (string.Equals(rootDir, restoreDir, PathUtility.PathComparison))
                    {
                        workTreePaths.Add(GetRestoreWorkTreeDir(restoreDir, workTreeHead));
                    }
                }
            }

            return workTreePaths;
        }
    }
}
