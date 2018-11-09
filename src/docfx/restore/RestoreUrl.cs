// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreUrl
    {
        public static async Task Restore(string address, Config config)
        {
            var tempFile = await DownloadToTempFile(address, config);

            var fileHash = HashUtility.GetFileSha1Hash(tempFile);
            var filePath = PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(address), fileHash));

            await ProcessUtility.RunInsideMutex(filePath, MoveFile);

            Task MoveFile()
            {
                if (!File.Exists(filePath))
                {
                    PathUtility.CreateDirectoryFromFilePath(filePath);
                    File.Move(tempFile, filePath);
                }
                else
                {
                    File.Delete(tempFile);
                }

                // update the last write date
                File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);

                return Task.CompletedTask;
            }
        }

        private static async Task<string> DownloadToTempFile(string address, Config config)
        {
            Directory.CreateDirectory(AppData.DownloadsDir);
            var tempFile = Path.Combine(AppData.DownloadsDir, "." + Guid.NewGuid().ToString("N"));

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
