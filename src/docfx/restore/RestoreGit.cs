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

        public static async Task<IEnumerable<(string href, string workTreeHead)>> Restore(string docsetPath, Config config, Func<string, Task> restoreChild)
        {
            var workTreeMappings = new ConcurrentBag<(string href, string workTreeHead)>();
            var restoreItems = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d)).Select(g => (g.Key, g.Distinct().ToList()));

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
                var workTreeHeads = await GetWorkTrees(docsetPath, restoreDir, hrefs);

                foreach (var (_, workTreeHead) in workTreeHeads)
                {
                    var childDir = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    await restoreChild(childDir);
                }

                return workTreeHeads;
            }
        }

        // Restore dependent repo to local and create work tree
        private static async Task<List<(string href, string head)>> GetWorkTrees(string docsetPath, string restoreDir, List<string> hrefs)
        {
            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            var (url, _) = GitUtility.GetGitRemoteInfo(hrefs.First());
            var workTreeHeads = new ConcurrentBag<(string href, string head)>();

            await ProcessUtility.CreateFileMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, restorePath)),
                async () =>
                {
                    await FetchOrCloneRepo();

                    var (existingWorkTrees, newWorkTrees) = await AddWorkTrees();

                    if (NeedCleanupWorkTrees(existingWorkTrees.Count))
                    {
                        using (Progress.Start($"Cleanup `{restoreDir}` work trees"))
                        {
                            await CleanupWorkTrees(existingWorkTrees, newWorkTrees);
                        }
                    }
                });

            async Task FetchOrCloneRepo()
            {
                if (GitUtility.IsRepo(restoreDir))
                {
                    // already exists, just pull the new updates from remote
                    await GitUtility.Fetch(restorePath);
                }
                else
                {
                    // doesn't exist yet, clone this repo to a specified branch
                    await GitUtility.Clone(restoreDir, url, restorePath, null, true);
                }
            }

            async Task<(List<string> existingWorkTrees, List<string> newWorkTrees)> AddWorkTrees()
            {
                var existingWorkTrees = new ConcurrentDictionary<string, int>((await GitUtility.ListWorkTrees(restorePath, false)).ToDictionary(k => k, v => 0));
                var newWorkTrees = new ConcurrentBag<string>();
                await ParallelUtility.ForEach(hrefs, async href =>
                {
                    var (_, rev) = GitUtility.GetGitRemoteInfo(href);
                    var workTreeHead = await GitUtility.Revision(restorePath, rev);
                    var workTreePath = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    if (existingWorkTrees.TryAdd(workTreePath, 0))
                    {
                        await GitUtility.AddWorkTree(restorePath, workTreeHead, workTreePath);
                    }

                    newWorkTrees.Add(workTreePath);
                    workTreeHeads.Add((href, workTreeHead));
                });

                return (existingWorkTrees.Keys.ToList(), newWorkTrees.ToList());
            }

            bool NeedCleanupWorkTrees(int existingWorkTreeCount) => existingWorkTreeCount > MaxWorkTreeCount;

            async Task CleanupWorkTrees(List<string> existingWorkTrees, List<string> newWorkTrees)
            {
                var allWorkTreesInUse = await GetAllWorkTreePaths(docsetPath, restoreDir);
                foreach (var newWorkTree in newWorkTrees)
                {
                    // add newly added work tree
                    allWorkTreesInUse.Add(newWorkTree);
                }

                foreach (var workTree in existingWorkTrees)
                {
                    if (!allWorkTreesInUse.Contains(workTree))
                    {
                        // remove not used work tree
                        Directory.Delete(workTree, true);
                    }
                }
                await GitUtility.PruneWorkTrees(restorePath);
            }

            Debug.Assert(hrefs.Count == workTreeHeads.Count);
            return workTreeHeads.ToList();
        }

        private static async Task<HashSet<string>> GetAllWorkTreePaths(string docsetPath, string restoreDir)
        {
            var allLocks = await RestoreLocker.LoadAll(
                file => !string.Equals(
                    PathUtility.NormalizeFile(file),
                    PathUtility.NormalizeFile(RestoreLocker.GetRestoreLockFilePath(docsetPath)),
                    PathUtility.PathComparison));
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
