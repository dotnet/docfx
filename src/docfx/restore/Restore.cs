// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Restore
    {
        private readonly RestoreLock _workTreeStore;

        public Restore(string docset)
        {
            _workTreeStore = new RestoreLock(docset);
        }

        public bool TryGetRestorePath(string remote, out string restorePath)
        {
            var (url, _) = GetGitRemoteInfo(remote);
            var restoreDir = GetRestoreDir(url);
            if (_workTreeStore.TryGetWorkTreeHead(remote, out var workTreeHead) && !string.IsNullOrEmpty(workTreeHead))
            {
                restorePath = GetWorkTreePath(restoreDir, workTreeHead);
                return true;
            }

            restorePath = default;
            return false;
        }

        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            using (Log.Measure("Restore dependencies"))
            {
                // Restore has to use Config directly, it cannot depend on Docset,
                // because Docset assumes the repo to physically exist on disk.
                var config = Config.Load(docsetPath, options);

                report.Configure(docsetPath, config);

                var restoredDirs = new HashSet<string>();
                var workTreeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                await ParallelUtility.ForEach(
                    config.Dependencies.Values,
                    async (href, restoreChild) =>
                    {
                        var workTreeHead = await RestoreDependentRepo(href, options, restoreChild, restoredDirs);
                        workTreeMappings[href] = workTreeHead;
                    },
                    progress: Log.Progress);

                if (workTreeMappings.Any())
                {
                    await RestoreLock.Lock(docsetPath, item =>
                    {
                        item.Git = workTreeMappings;
                        return item;
                    });
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

        // Recursively restore dependent repo including their children
        private static async Task<string> RestoreDependentRepo(string href, CommandLineOptions options, Action<string> restoreChild, HashSet<string> restoredDirs)
        {
            var (childDir, workTreeHead) = await FetchOrCloneDependentRepo(href);

            if (restoredDirs.Add(childDir) && Config.LoadIfExists(childDir, options, out var childConfig))
            {
                foreach (var (key, childHref) in childConfig.Dependencies)
                {
                    restoreChild(childHref);
                }
            }

            return workTreeHead;
        }

        // Fetch or clone dependent repo to local
        private static async Task<(string workTreePath, string workTreeHead)> FetchOrCloneDependentRepo(string href)
        {
            var (url, rev) = GetGitRemoteInfo(href);
            var restoreDir = GetRestoreDir(url);
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
                    await GitUtility.PruneWorkTrees(restorePath);
                    workTreePath = GetWorkTreePath(restoreDir, workTreeHead);
                    var workTrees = await GitUtility.ListWorkTrees(restorePath);
                    if (!workTrees.Contains(workTreePath))
                    {
                        await GitUtility.CreateWorkTree(restorePath, workTreeHead, workTreePath);
                    }
                });

            Debug.Assert(!string.IsNullOrEmpty(workTreeHead));
            Debug.Assert(!string.IsNullOrEmpty(workTreePath));

            return (workTreePath, workTreeHead);
        }

        private static string GetWorkTreePath(string restoreDir, string workTreeHead)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, workTreeHead));

        private static string GetRestoreDir(string url)
        {
            var uri = new Uri(url);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(AppData.RestoreDir, repo);
            return PathUtility.NormalizeFolder(dir);
        }
    }
}
