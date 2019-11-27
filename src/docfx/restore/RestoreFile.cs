// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal static class RestoreFile
    {
        private static readonly HttpClient s_httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        public static Task Restore(List<string> urls, Config config)
        {
            return ParallelUtility.ForEach(
                urls,
                restoreUrl => Restore(restoreUrl, config));
        }

        public static async Task Restore(string url, Config config)
        {
            var filePath = GetRestorePathFromUrl(url);
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
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));

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
            yield return config.MarkdownValidationRules;

            foreach (var metadataSchema in config.MetadataSchema)
            {
                yield return metadataSchema;
            }
        }

        public static string GetRestorePathFromUrl(string url)
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
                using (PerfScope.Start($"Downloading '{url}'"))
                using (var response = await HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<OperationCanceledException>()
                    .Or<IOException>()
                    .RetryAsync(3)
                    .ExecuteAsync(() => GetAsync(url, config, existingEtag)))
                {
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
            }
            catch (Exception ex)
            {
                throw Errors.DownloadFailed(url).ToException(ex);
            }
        }

        private static Task<HttpResponseMessage> GetAsync(string url, Config config, EntityTagHeaderValue etag = null)
        {
            // Create new instance of HttpRequestMessage to avoid System.InvalidOperationException:
            // "The request message was already sent. Cannot send the same request message multiple times."
            using (var message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                AddAuthorizationHeader(url, message, config);

                if (etag != null)
                {
                    message.Headers.IfNoneMatch.Add(etag);
                }
                return s_httpClient.SendAsync(message);
            }
        }

        private static void AddAuthorizationHeader(string url, HttpRequestMessage message, Config config)
        {
            foreach (var (baseUrl, rule) in config.Http)
            {
                if (url.StartsWith(baseUrl))
                {
                    foreach (var header in rule.Headers)
                    {
                        message.Headers.Add(header.Key, header.Value);
                    }
                    break;
                }
            }
        }
    }
}
