// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class FileResolver
    {
        // NOTE: This line assumes each build runs in a new process
        private static readonly ConcurrentDictionary<(string downloadsRoot, string), Lazy<string>> s_urls
                          = new ConcurrentDictionary<(string, string), Lazy<string>>();

        private static readonly HttpClient s_httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        private readonly Lazy<string?>? _fallbackDocsetPath;
        private readonly Action<HttpRequestMessage>? _credentialProvider;
        private readonly OpsConfigAdapter? _opsConfigAdapter;
        private readonly FetchOptions _fetchOptions;
        private readonly Package _package;

        public FileResolver(
            Package package,
            Lazy<string?>? fallbackDocsetPath = null,
            Action<HttpRequestMessage>? credentialProvider = null,
            OpsConfigAdapter? opsConfigAdapter = null,
            FetchOptions fetchOptions = default)
        {
            _package = package;
            _fallbackDocsetPath = fallbackDocsetPath;
            _opsConfigAdapter = opsConfigAdapter;
            _fetchOptions = fetchOptions;
            _credentialProvider = credentialProvider;
        }

        public string? TryReadString(SourceInfo<string> file)
        {
            try
            {
                return ReadString(file);
            }
            catch (DocfxException)
            {
                return null;
            }
        }

        public string ReadString(SourceInfo<string> file)
        {
            using var reader = new StreamReader(ReadStream(file));
            return reader.ReadToEnd();
        }

        public Stream ReadStream(SourceInfo<string> file)
        {
            if (UrlUtility.IsHttp(file))
            {
                var content = TestQuirks.HttpProxy?.Invoke(file);
                if (content != null)
                {
                    var byteArray = Encoding.ASCII.GetBytes(content);
                    return new MemoryStream(byteArray);
                }
            }
            return _package.ReadStream(new PathString(ResolveFilePath(file)));
        }

        public bool TryResolveFilePath(SourceInfo<string> file, out string? result)
        {
            try
            {
                result = ResolveFilePath(file);
                return true;
            }
            catch (DocfxException ex) when (ex.Error.Code == "file-not-found" || ex.Error.Code == "toc-not-found" || ex.Error.Code == "download-failed")
            {
                result = default;
                return false;
            }
        }

        public string ResolveFilePath(SourceInfo<string> file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return file;
            }

            if (!UrlUtility.IsHttp(file))
            {
                var localFilePath = _package.TryGetFullFilePath(new PathString(file));
                if (localFilePath != null)
                {
                    return localFilePath;
                }
                else if (_fallbackDocsetPath?.Value != null)
                {
                    localFilePath = _package.TryGetFullFilePath(new PathString(Path.Combine(_fallbackDocsetPath.Value, file)));
                    if (localFilePath != null)
                    {
                        return localFilePath;
                    }
                }

                throw Errors.Link.FileNotFound(file).ToException();
            }

            var content = TestQuirks.HttpProxy?.Invoke(file);
            if (content != null)
            {
                return file;
            }
            return DownloadFromUrl(file);
        }

        public void Download(SourceInfo<string> file)
        {
            if (UrlUtility.IsHttp(file))
            {
                DownloadFromUrl(file);
            }
        }

        private string DownloadFromUrl(SourceInfo<string> url)
        {
            return s_urls.GetOrAdd((AppData.DownloadsRoot, url), _ => new Lazy<string>(() => DownloadFromUrlCore(url))).Value;
        }

        private string DownloadFromUrlCore(string url)
        {
            var filePath = GetRestorePathFromUrl(url);

            switch (_fetchOptions)
            {
                case FetchOptions.UseCache when _package.Exists(filePath):
                case FetchOptions.NoFetch when _package.Exists(filePath):
                    return filePath;

                case FetchOptions.NoFetch:
                    throw Errors.System.NeedRestore(url).ToException();
            }

            var etagPath = GetRestoreEtagPath(url);
            var existingEtag = default(EntityTagHeaderValue);

            var etagContent = _package.Exists(etagPath) ? _package.ReadString(etagPath) : null;
            if (!string.IsNullOrEmpty(etagContent))
            {
                existingEtag = EntityTagHeaderValue.Parse(_package.ReadString(etagPath));
            }

            var (tempFile, etag) = DownloadToTempFile(url, existingEtag).GetAwaiter().GetResult();
            if (tempFile is null)
            {
                // no change at all
                return filePath;
            }

            using (InterProcessMutex.Create(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));

                File.Move(tempFile, filePath, overwrite: true);

                if (etag != null)
                {
                    File.WriteAllText(etagPath, etag.ToString());
                }
                return filePath;
            }
        }

        private static PathString GetRestorePathFromUrl(string url)
        {
            return new PathString(Path.Combine(AppData.GetFileDownloadPath(url)));
        }

        private static PathString GetRestoreEtagPath(string url)
        {
            return new PathString(Path.Combine(AppData.GetFileDownloadPath(url) + ".etag"));
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
                throw Errors.System.DownloadFailed(url).ToException(ex);
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
