// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class GarbageCollector
    {
        private const int MaxKeepingDays = 15;

        public static async Task Collect(int notAccessedForDays)
        {
            notAccessedForDays = notAccessedForDays <= 0 ? MaxKeepingDays : notAccessedForDays;
            await CollectGit(notAccessedForDays);
            CollectUrls(notAccessedForDays);
        }

        private static async Task CollectGit(int notAccessedForDays)
        {
            if (!Directory.Exists(AppData.GitRestoreDir))
            {
                return;
            }

            var gitWorkTreeRoots = Directory.EnumerateDirectories(AppData.GitRestoreDir, ".git", SearchOption.AllDirectories);

            using (Progress.Start("GC dependency git repositories"))
            {
                await ParallelUtility.ForEach(
                    gitWorkTreeRoots,
                    async gitWorkTreeRoot =>
                    {
                        await ProcessUtility.RunInsideMutex(
                           PathUtility.NormalizeFile(Path.GetRelativePath(AppData.GitRestoreDir, gitWorkTreeRoot)),
                           async () =>
                           {
                               var workTreeFolder = Path.GetDirectoryName(gitWorkTreeRoot);
                               var existingWorkTreeFolders = Directory.EnumerateDirectories(workTreeFolder, "*", SearchOption.TopDirectoryOnly)
                                                          .Select(f => PathUtility.NormalizeFolder(f)).Where(f => !f.EndsWith(".git/")).ToList();

                               foreach (var existingWorkTreeFolder in existingWorkTreeFolders)
                               {
                                   if (new DirectoryInfo(existingWorkTreeFolder).LastAccessTimeUtc + TimeSpan.FromDays(notAccessedForDays) < DateTime.UtcNow)
                                   {
                                       Directory.Delete(existingWorkTreeFolder, true);
                                   }
                               }

                               await GitUtility.PruneWorkTrees(workTreeFolder);
                           });
                    },
                    Progress.Update);
            }
        }

        private static void CollectUrls(int notAccessedForDays)
        {
            if (!Directory.Exists(AppData.UrlRestoreDir))
            {
                return;
            }

            var downloadedFiles = Directory.EnumerateFiles(AppData.UrlRestoreDir, "*", SearchOption.AllDirectories);

            using (Progress.Start("GC downloaded urls"))
            {
                ParallelUtility.ForEach(
                    downloadedFiles,
                    downloadedFile =>
                    {
                        if (new FileInfo(downloadedFile).LastAccessTimeUtc + TimeSpan.FromDays(notAccessedForDays) < DateTime.UtcNow)
                        {
                            File.Delete(downloadedFile);
                        }
                    },
                    Progress.Update);
            }
        }
    }
}
