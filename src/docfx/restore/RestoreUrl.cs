// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreUrl
    {
        public static string GetRestoreVersionPath(string restoreDir, string version)
            => PathUtility.NormalizeFile(Path.Combine(restoreDir, version));

        public static string GetRestoreRootDir(string address)
            => Docs.Build.Restore.GetRestoreRootDir(address, AppData.UrlRestoreDir);

        public static async Task<string> Restore(string address, Config config)
        {
            var tempFile = await DownloadToTempFile(address, config);

            var fileVersion = "";
            using (var fileStream = File.Open(tempFile, FileMode.Open, FileAccess.Read))
            {
                fileVersion = HashUtility.GetSha1Hash(fileStream);
            }

            Debug.Assert(!string.IsNullOrEmpty(fileVersion));

            var restoreDir = GetRestoreRootDir(address);
            var restorePath = GetRestoreVersionPath(restoreDir, fileVersion);
            await ProcessUtility.RunInsideMutex(
                PathUtility.NormalizeFile(Path.GetRelativePath(AppData.UrlRestoreDir, restoreDir)),
                () =>
                {
                    if (!File.Exists(restorePath))
                    {
                        PathUtility.CreateDirectoryFromFilePath(restorePath);
                        File.Move(tempFile, restorePath);
                    }
                    else
                    {
                        File.Delete(tempFile);
                    }

                    // update the last write date
                    File.SetLastWriteTimeUtc(restorePath, DateTime.UtcNow);

                    return Task.CompletedTask;
                });

            return fileVersion;
        }

        private static async Task<string> DownloadToTempFile(string address, Config config)
        {
            Directory.CreateDirectory(AppData.UrlRestoreDir);
            var tempFile = Path.Combine(AppData.UrlRestoreDir, "." + Guid.NewGuid().ToString("N"));

            try
            {
                var response = await HttpClientUtility.GetAsync(address, config);

                using (var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync())
                using (var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(file);
                }
            }
            catch (HttpRequestException ex)
            {
                throw Errors.DownloadFailed(address, ex.Message).ToException(ex);
            }
            return tempFile;
        }
    }
}
