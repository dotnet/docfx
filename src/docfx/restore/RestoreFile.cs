// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreFile
    {
        public static Task Restore(List<string> urls, Config config, bool @implicit = false)
        {
            return ParallelUtility.ForEach(
                   urls,
                   restoreUrl =>
                   {
                       return Restore(restoreUrl, config, @implicit);
                   });
        }

        public static async Task Restore(string url, Config config, bool @implicit = false)
        {
            var filePath = GetRestorePath(url);

            var (existingContent, existingEtagContent) = await RestoreMap.TryGetRestoredFileContent(url);
            if (!string.IsNullOrEmpty(existingContent) && @implicit)
                return;

            await ProcessUtility.RunInsideMutex(filePath, async () =>
            {
                var existingEtag = !string.IsNullOrEmpty(existingEtagContent) ? new EntityTagHeaderValue(existingEtagContent) : null;

                var (tempFile, etag) = await DownloadToTempFile(url, config, existingEtag);
                if (tempFile == null)
                {
                    // no change at all
                    return;
                }

                if (!File.Exists(filePath))
                {
                    PathUtility.CreateDirectoryFromFilePath(filePath);
                    File.Move(tempFile, filePath);
                }
                else
                {
                    File.Delete(tempFile);
                }

                File.WriteAllText($"{filePath}.etag", etag?.ToString());
            });
        }

        public static IEnumerable<string> GetFileReferences(this Config config)
        {
            foreach (var url in config.Xref)
            {
                yield return url;
            }

            yield return config.Contribution.GitCommitsTime;
            yield return config.GitHub.UserCache;
            yield return config.MonikerDefinition;
        }

        public static string GetRestorePath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            return PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(url), HashUtility.GetMd5HashShort(url)));
        }

        private static async Task<(string filename, EntityTagHeaderValue etag)> DownloadToTempFile(
            string url,
            Config config,
            EntityTagHeaderValue existingEtag)
        {
            try
            {
                var response = await HttpClientUtility.GetAsync(url, config, existingEtag);
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    return (null, existingEtag);
                }

                Directory.CreateDirectory(AppData.DownloadsRoot);
                var tempFile = Path.Combine(AppData.DownloadsRoot, "." + Guid.NewGuid().ToString("N"));

                using (var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync())
                using (var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(file);
                }
                return (tempFile, response.Headers.ETag);
            }
            catch (Exception ex)
            {
                throw Errors.DownloadFailed(url, ex.Message).ToException(ex);
            }
        }
    }
}
