// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    public static class RestoreUrl
    {
        private const int MaxVersionCount = 5;
        private const int RetryCount = 3;
        private const int RetryInterval = 1000;

        private static readonly HttpClient s_httpClient = new HttpClient();

        public static string GetRestoreVersionPath(string restoreDir, string version)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, version));

        public static string GetRestoreRootDir(string address)
            => Docs.Build.Restore.GetRestoreRootDir(address, AppData.UrlRestoreDir);

        public static async Task<string> Restore(string address)
        {
            var tempFile = await DownloadToTempFile(address);

            var fileVersion = "";
            using (var fileStream = File.Open(tempFile, FileMode.Open, FileAccess.Read))
            {
                fileVersion = HashUtility.GetSha1HashString(fileStream);
            }

            Debug.Assert(!string.IsNullOrEmpty(fileVersion));

            var restoreDir = GetRestoreRootDir(address);
            var restorePath = GetRestoreVersionPath(restoreDir, fileVersion);
            await ProcessUtility.CreateFileMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.UrlRestoreDir, restoreDir)),
                () =>
                {
                    if (!File.Exists(restorePath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(restorePath));
                        File.Move(tempFile, restorePath);
                    }

                    return Task.CompletedTask;
                });

            return fileVersion;
        }

        public static async Task GC(string address)
        {
            var restoreDir = GetRestoreRootDir(address);
            if (!Directory.Exists(restoreDir))
            {
                return;
            }

            await ProcessUtility.CreateFileMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.UrlRestoreDir, restoreDir)),
                async () =>
                {
                    var existingVersionPaths = Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                                               .Select(f => PathUtility.NormalizeFile(f)).ToList();

                    if (NeedCleanupVersions(existingVersionPaths.Count))
                    {
                        await CleanupVersions(restoreDir, existingVersionPaths);
                    }
                });

            bool NeedCleanupVersions(int versionCount) => versionCount > MaxVersionCount;

            async Task CleanupVersions(string root, List<string> existingVersionPaths)
            {
                var inUseVersionPaths = await GetAllVersionPaths(root);

                foreach (var existingVersionPath in existingVersionPaths)
                {
                    if (!inUseVersionPaths.Contains(existingVersionPath, PathUtility.PathComparer))
                    {
                        File.Delete(existingVersionPath);
                    }
                }
            }
        }

        private static async Task<string> DownloadToTempFile(string address)
        {
            Directory.CreateDirectory(AppData.UrlRestoreDir);
            var tempFile = Path.Combine(AppData.UrlRestoreDir, "." + Guid.NewGuid().ToString("N"));

            for (var i = 0; i < RetryCount; i++)
            {
                var response = await s_httpClient.GetAsync(new Uri(address));
                if (!response.IsSuccessStatusCode)
                {
                    if (i < RetryCount - 1)
                    {
                        await Task.Delay(RetryInterval);
                        continue;
                    }
                    throw Errors.DownloadFailed(address, (int)response.StatusCode).ToException();
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(file);
                }
                return tempFile;
            }

            Debug.Fail("should never reach here");
            return tempFile;
        }

        private static async Task<HashSet<string>> GetAllVersionPaths(string restoreDir)
        {
            var allLocks = await RestoreLocker.LoadAll();
            var versionPath = new HashSet<string>();

            foreach (var restoreLock in allLocks)
            {
                foreach (var (href, version) in restoreLock.Url)
                {
                    var rootDir = GetRestoreRootDir(href);
                    if (string.Equals(rootDir, restoreDir, PathUtility.PathComparison))
                    {
                        versionPath.Add(GetRestoreVersionPath(restoreDir, version));
                    }
                }
            }

            return versionPath;
        }
    }
}
