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
        public static Task Restore(List<string> urls, Config config)
        {
            return ParallelUtility.ForEach(
                urls,
                restoreUrl => Restore(restoreUrl, config));
        }

        public static async Task Restore(string url, Config config)
        {
            Console.WriteLine($"Downloading '{url}'");

            var filePath = GetRestoreContentPath(url);
            var etagPath = GetRestoreEtagPath(url);
            var existingEtag = default(EntityTagHeaderValue);

            using (InterProcessMutex.Create(filePath))
            {
                var etagContent = File.Exists(etagPath) ? File.ReadAllText(etagPath) : null;
                if (!string.IsNullOrEmpty(etagContent))
                {
                    existingEtag = EntityTagHeaderValue.Parse(File.ReadAllText(etagPath));
                }
            }

            var (tempFile, etag) = await DownloadToTempFile(url, config, existingEtag);
            if (tempFile is null)
            {
                // no change at all
                return;
            }

            using (InterProcessMutex.Create(filePath))
            {
                PathUtility.CreateDirectoryFromFilePath(filePath);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempFile, filePath);

                if (etag != null)
                {
                    File.WriteAllText(etagPath, etag.ToString());
                }
            }
        }

        public static IEnumerable<string> GetFileReferences(this Config config)
        {
            foreach (var url in config.Xref)
            {
                yield return url;
            }

            yield return config.MonikerDefinition;

            foreach (var metadataSchema in config.MetadataSchema)
            {
                yield return metadataSchema;
            }
        }

        public static string GetRestoreContentPath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(UrlUtility.IsHttp(url));

            return PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(url), "content"));
        }

        public static string GetRestoreEtagPath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(UrlUtility.IsHttp(url));

            return PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(url), "etag"));
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
                throw Errors.DownloadFailed(url).ToException(ex);
            }
        }
    }
}
