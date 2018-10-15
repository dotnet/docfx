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
        private const int MaxWorkTreeCount = 5;

        public static string GetRestoreWorkTreeDir(string restoreDir, string workTreeHead)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        public static async Task<List<(string href, string head)>> AddWorkTrees(string restoreDir, List<string> hrefs, string token)
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
                if (GitUtility.IsRepo(restoreDir))
                {
                    // already exists, just pull the new updates from remote
                    // fetch bare repo: https://stackoverflow.com/questions/3382679/how-do-i-update-my-bare-repo
                    return GitUtility.Fetch(restorePath, url, "+refs/heads/*:refs/heads/*", token);
                }
                else
                {
                    // doesn't exist yet, clone this repo to a specified branch
                    return GitUtility.Clone(restoreDir, url, restorePath, token: token, bare: true);
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

        public static async Task<HashSet<string>> GetAllWorkTreePaths(string restoreDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));

            var allLocks = await RestoreLocker.LoadAll();
            var workTreePaths = new HashSet<string>();

            foreach (var restoreLock in allLocks)
            {
                foreach (var (href, workTreeHead) in restoreLock.Git)
                {
                    var rootDir = RestoreGit.GetRestoreRootDir(href);
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
