// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        private static readonly int s_maxWorkTreeCount = 5;
        private static readonly int s_maxAccessDurationInDays = 5;

        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            using (Progress.Start("Restore dependencies"))
            {
                // Restore has to use Config directly, it cannot depend on Docset,
                // because Docset assumes the repo to physically exist on disk.
                var config = Config.Load(docsetPath, options);

                report.Configure(docsetPath, config);

                var restoredDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);
                var restoreLockMaps = new ConcurrentDictionary<string, RestoreLock>(PathUtility.PathComparer);

                await RestoreDocset(docsetPath);

                async Task RestoreDocset(string docset)
                {
                    if (restoredDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out var docsetConfig))
                    {
                        var docsetLock = await RestoreOneDocset(docsetConfig, RestoreDocset);
                        restoreLockMaps.TryAdd(docset, docsetLock);
                    }
                }

                foreach (var (docset, restoreLock) in restoreLockMaps)
                {
                    await RestoreLocker.Lock(docset, restoreLock);
                }
            }
        }

        public static (string url, string branch) GetGitRemoteInfo(string remoteHref)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteHref));

            var (path, _, fragment) = HrefUtility.SplitHref(remoteHref);

            var refSpec = (string.IsNullOrEmpty(fragment) || fragment.Length <= 1) ? "master" : fragment.Substring(1);
            var uri = new Uri(path);
            var url = uri.GetLeftPart(UriPartial.Path);

            return (url, refSpec);
        }

        public static string GetRestoreWorkTreeDir(string restoreDir, string workTreeHead)
        => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        public static string GetRestoreRootDir(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var uri = new Uri(url);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(AppData.RestoreDir, repo);
            return PathUtility.NormalizeFolder(dir);
        }

        private static async Task<RestoreLock> RestoreOneDocset(Config config, Func<string, Task> restoreChild)
        {
            var workTreeMappings = new ConcurrentBag<(string href, string workTreeHead)>();

            // process git restore items
            await ParallelUtility.ForEach(
               config.Dependencies.Values,
               async href =>
               {
                   var workTreeHead = await RestoreDependentRepo(href);
                   workTreeMappings.Add((href, workTreeHead));
               },
               progress: Progress.Update);

            var result = new RestoreLock();
            foreach (var (href, workTreeHead) in workTreeMappings)
            {
                result.Git[href] = workTreeHead;
            }

            // todo: restore other items, @renze
            return result;

            async Task<string> RestoreDependentRepo(string href)
            {
                var (childDir, workTreeHead) = await FetchOrCloneDependentRepo(href);
                await restoreChild(childDir);

                return workTreeHead;
            }
        }

        // Fetch or clone dependent repo to local and create work tree
        private static async Task<(string workTreePath, string workTreeHead)> FetchOrCloneDependentRepo(string href)
        {
            var (url, rev) = GetGitRemoteInfo(href);
            var restoreDir = GetRestoreRootDir(url);
            var restorePath = PathUtility.NormalizeFolder(Path.Combine(restoreDir, ".git"));
            var workTreeHead = string.Empty;
            var workTreePath = string.Empty;

            var lockRelativePath = Path.Combine(Path.GetRelativePath(AppData.RestoreDir, restoreDir), ".lock");
            await ProcessUtility.ProcessLock(
                lockRelativePath,
                async () =>
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

                    workTreeHead = await GitUtility.Revision(restorePath, rev);
                    workTreePath = GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    var workTrees = new HashSet<string>(await GitUtility.ListWorkTrees(restorePath, false));
                    if (workTrees.Add(workTreePath))
                    {
                        await GitUtility.AddWorkTree(restorePath, workTreeHead, workTreePath);
                    }

                    if (workTrees.Count > s_maxWorkTreeCount)
                    {
                        var allWorkTreesInUse = await GetAllWorkTreePaths(restoreDir);
                        foreach (var workTree in workTrees)
                        {
                            if (!allWorkTreesInUse.Contains(workTree))
                            {
                                await GitUtility.RemoveWorkTree(restorePath, workTree);
                            }
                        }
                        await GitUtility.PruneWorkTrees(restorePath);
                    }
                });

            Debug.Assert(!string.IsNullOrEmpty(workTreeHead));
            Debug.Assert(!string.IsNullOrEmpty(workTreePath));

            return (workTreePath, workTreeHead);
        }

        private static async Task<HashSet<string>> GetAllWorkTreePaths(string restoreDir)
        {
            var allLocks = await RestoreLocker.LoadAll();
            var workTreePaths = new HashSet<string>();

            foreach (var restoreLock in allLocks)
            {
                var interval = DateTime.UtcNow - restoreLock.LastAccessData;
                if (interval.TotalDays > s_maxAccessDurationInDays)
                {
                    // The lock file was not used for a long time, just ignore
                    continue;
                }

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
