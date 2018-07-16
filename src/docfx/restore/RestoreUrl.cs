// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static string GetRestoreVersionPath(string restoreDir, string version)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, version));

        public static string GetRestoreRootDir(string address)
            => Docs.Build.Restore.GetRestoreRootDir(address, AppData.UrlRestoreDir);

        public static async Task<string> Restore(string docset, string address)
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
                async () =>
                {
                    if (!File.Exists(restorePath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(restorePath));
                        File.Move(tempFile, restorePath);
                    }

                    var existingVersionPaths = Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                                               .Select(f => PathUtility.NormalizeFile(f)).ToList();

                    if (NeedCleanupVersions(existingVersionPaths.Count))
                    {
                        await CleanupVersions(restoreDir, restorePath, existingVersionPaths);
                    }
                });

            return fileVersion;

            bool NeedCleanupVersions(int versionCount) => versionCount > MaxVersionCount;

            async Task CleanupVersions(string root, string newVersionPath, List<string> existingVersionPaths)
            {
                var inUseVersionPaths = await GetAllVersionPaths(docset, root);
                inUseVersionPaths.Add(newVersionPath);

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
            var tempFile = Path.GetTempFileName();
            using (var client = new HttpClient())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempFile));
                using (var request = new HttpRequestMessage(HttpMethod.Get, address))
                {
                    using (Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }

            return tempFile;
        }

        private static async Task<HashSet<string>> GetAllVersionPaths(string docsetPath, string restoreDir)
        {
            var allLocks = await RestoreLocker.LoadAll(
                file => !string.Equals(
                    PathUtility.NormalizeFile(file),
                    PathUtility.NormalizeFile(RestoreLocker.GetRestoreLockFilePath(docsetPath)),
                    PathUtility.PathComparison));
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
