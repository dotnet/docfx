// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class FileResolver
    {
        private static readonly HttpClient s_httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        private readonly string _docsetPath;
        private readonly Action<HttpRequestMessage>? _credentialProvider;
        private readonly OpsConfigAdapter? _opsConfigAdapter;
        private readonly FetchOptions _fetchOptions;
        private readonly Docset? _fallbackDocset;

        public FileResolver(
            string docsetPath,
            Action<HttpRequestMessage>? credentialProvider = null,
            OpsConfigAdapter? opsConfigAdapter = null,
            FetchOptions fetchOptions = default,
            Docset? fallbackDocset = null)
        {
            _docsetPath = docsetPath;
            _opsConfigAdapter = opsConfigAdapter;
            _fetchOptions = fetchOptions;
            _credentialProvider = credentialProvider;
            _fallbackDocset = fallbackDocset;
        }

        public string ReadString(SourceInfo<string> file)
        {
            using var reader = new StreamReader(ReadStream(file));
            return reader.ReadToEnd();
        }

        public Stream ReadStream(SourceInfo<string> file)
        {
            if (_fetchOptions != FetchOptions.NoFetch)
            {
                Download(file).GetAwaiter().GetResult();
            }

            if (!UrlUtility.IsHttp(file))
            {
                var localFilePath = Path.Combine(_docsetPath, file);
                if (File.Exists(localFilePath))
                {
                    return File.OpenRead(localFilePath);
                }
                else if (_fallbackDocset != null
                    && File.Exists(localFilePath = Path.Combine(_fallbackDocset.DocsetPath, file)))
                {
                    return File.OpenRead(localFilePath);
                }

                throw Errors.FileNotFound(file).ToException();
            }

            var filePath = GetRestorePathFromUrl(file);
            if (!File.Exists(filePath))
            {
                throw Errors.NeedRestore(file).ToException();
            }

            return File.OpenRead(filePath);
        }

        public async Task Download(SourceInfo<string> file)
        {
            if (!UrlUtility.IsHttp(file))
            {
                return;
            }

            if (_fetchOptions == FetchOptions.NoFetch)
            {
                throw Errors.NeedRestore(file).ToException();
            }

            var filePath = GetRestorePathFromUrl(file);
            var etagPath = GetRestoreEtagPath(file);
            var existingEtag = default(EntityTagHeaderValue);

            using (InterProcessMutex.Create(filePath))
            {
                if (_fetchOptions == FetchOptions.UseCache && File.Exists(filePath))
                {
                    return;
                }

                var etagContent = File.Exists(etagPath) ? File.ReadAllText(etagPath) : null;
                if (!string.IsNullOrEmpty(etagContent))
                {
                    existingEtag = EntityTagHeaderValue.Parse(File.ReadAllText(etagPath));
                }
            }

            var (tempFile, etag) = await DownloadToTempFile(file, existingEtag);
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

            return;
        }

        private static string GetRestorePathFromUrl(string url)
        {
            return PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(url), "content"));
        }

        private static string GetRestoreEtagPath(string url)
        {
            return PathUtility.NormalizeFile(Path.Combine(AppData.GetFileDownloadDir(url), "etag"));
        }

        private async Task<(string? filename, EntityTagHeaderValue? etag)> DownloadToTempFile(
            string url, EntityTagHeaderValue? existingEtag)
        {
            try
            {
                using (PerfScope.Start($"Downloading '{url}'"))
                using (var response = await HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<OperationCanceledException>()
                    .Or<IOException>()
                    .RetryAsync(3, onRetry: (_, i) => Log.Write($"[{i}] Retrying '{url}'"))
                    .ExecuteAsync(() => GetAsync(url, existingEtag)))
                {
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        return (null, existingEtag);
                    }

                    Directory.CreateDirectory(AppData.DownloadsRoot);
                    var tempFile = Path.Combine(AppData.DownloadsRoot, "." + Guid.NewGuid().ToString("N"));

                    using var stream = await response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync();
                    using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(file);
                    return (tempFile, response.Headers.ETag);
                }
            }
            catch (Exception ex) when (!DocfxException.IsDocfxException(ex, out _))
            {
                throw Errors.DownloadFailed(url).ToException(ex);
            }
        }

        private async Task<HttpResponseMessage> GetAsync(string url, EntityTagHeaderValue? etag = null)
        {
            // Create new instance of HttpRequestMessage to avoid System.InvalidOperationException:
            // "The request message was already sent. Cannot send the same request message multiple times."
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            if (etag != null)
            {
                message.Headers.IfNoneMatch.Add(etag);
            }

            _credentialProvider?.Invoke(message);

            if (_opsConfigAdapter != null)
            {
                var response = await _opsConfigAdapter.InterceptHttpRequest(message);
                if (response != null)
                {
                    return response;
                }
            }

            return await s_httpClient.SendAsync(message);
        }
    }
}
