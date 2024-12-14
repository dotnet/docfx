// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Net;
using Docfx.Common;

using EnvironmentVariables = Docfx.DataContracts.Common.Constants.EnvironmentVariables;

namespace Docfx.Build.Engine;

public sealed class XRefMapDownloader
{
    private readonly SemaphoreSlim _semaphore;
    private readonly IReadOnlyList<string> _localFileFolders;

    public XRefMapDownloader(string baseFolder = null, IReadOnlyList<string> fallbackFolders = null, int maxParallelism = 0x10)
    {
        _semaphore = new SemaphoreSlim(maxParallelism);
        if (baseFolder == null)
        {
            baseFolder = Directory.GetCurrentDirectory();
        }
        else
        {
            baseFolder = Path.Combine(Directory.GetCurrentDirectory(), baseFolder);
        }
        var localFileFolders = new List<string>() { baseFolder };
        if (fallbackFolders != null)
        {
            localFileFolders.AddRange(fallbackFolders);
        }
        _localFileFolders = localFileFolders;
    }

    /// <summary>
    /// Download xref map file from uri (async).
    /// </summary>
    /// <param name="uri">The uri of xref map file.</param>
    /// <returns>An instance of <see cref="XRefMap"/>.</returns>
    /// <threadsafety>This method is thread safe.</threadsafety>
    public async Task<IXRefContainer> DownloadAsync(Uri uri, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        await _semaphore.WaitAsync(token);
        return await Task.Run(async () =>
        {
            try
            {
                if (uri.IsAbsoluteUri)
                {
                    return await DownloadBySchemeAsync(uri, token);
                }
                else
                {
                    return await ReadLocalFileWithFallback(uri, token);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    private ValueTask<IXRefContainer> ReadLocalFileWithFallback(Uri uri, CancellationToken token = default)
    {
        foreach (var localFileFolder in _localFileFolders)
        {
            var localFilePath = Path.Combine(localFileFolder, uri.OriginalString);
            if (File.Exists(localFilePath))
            {
                return ReadLocalFileAsync(localFilePath, token);
            }
        }
        throw new FileNotFoundException($"Cannot find xref map file {uri.OriginalString} in path: {string.Join(',', _localFileFolders)}", uri.OriginalString);
    }

    /// <remarks>
    /// Support scheme: http, https, file.
    /// </remarks>
    private async ValueTask<IXRefContainer> DownloadBySchemeAsync(Uri uri, CancellationToken token = default)
    {
        IXRefContainer result;
        if (uri.IsFile)
        {
            result = await DownloadFromLocalAsync(uri, token);
        }
        else if (uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps)
        {
            result = await DownloadFromWebAsync(uri, token);
        }
        else
        {
            throw new ArgumentException($"Unsupported scheme {uri.Scheme}, expected: http, https, file.", nameof(uri));
        }
        if (result == null)
        {
            throw new InvalidDataException($"Invalid yaml file from {uri}.");
        }
        return result;
    }

    private static ValueTask<IXRefContainer> DownloadFromLocalAsync(Uri uri, CancellationToken token = default)
    {
        var filePath = uri.LocalPath;
        return ReadLocalFileAsync(filePath, token);
    }

    private static async ValueTask<IXRefContainer> ReadLocalFileAsync(string filePath, CancellationToken token = default)
    {
        Logger.LogVerbose($"Reading from file: {filePath}");

        switch (Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".zip":
                return XRefArchive.Open(filePath, XRefArchiveMode.Read);

            case ".gz":
                {
                    using var fileStream = File.OpenRead(filePath);
                    using var stream = new GZipStream(fileStream, CompressionMode.Decompress);

                    switch (Path.GetExtension(Path.GetFileNameWithoutExtension(filePath)).ToLowerInvariant())
                    {
                        case ".json":
                            return await JsonUtility.DeserializeAsync<XRefMap>(stream, token);
                        case ".yml":
                        default:
                            {
                                using var reader = new StreamReader(stream);
                                return YamlUtility.Deserialize<XRefMap>(reader);
                            }
                    }
                }

            case ".json":
                {
                    using var stream = File.OpenRead(filePath);
                    return await JsonUtility.DeserializeAsync<XRefMap>(stream, token);
                }

            case ".yml":
            default:
                {
                    return YamlUtility.Deserialize<XRefMap>(filePath);
                }
        }
    }

    private static async Task<XRefMap> DownloadFromWebAsync(Uri uri, CancellationToken token = default)
    {
        Logger.LogVerbose($"Reading from web: {uri.OriginalString}");

        using var httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All,
            CheckCertificateRevocationList = !EnvironmentVariables.NoCheckCertificateRevocationList,
        })
        {
            Timeout = TimeSpan.FromMinutes(30), // Default: 100 seconds
        };

        using var stream = await httpClient.GetStreamAsync(uri, token);

        switch (Path.GetExtension(uri.AbsolutePath).ToLowerInvariant())
        {
            case ".json":
                {
                    var xrefMap = await JsonUtility.DeserializeAsync<XRefMap>(stream, token);
                    xrefMap.BaseUrl = ResolveBaseUrl(xrefMap, uri);
                    return xrefMap;
                }
            case ".yml":
            default:
                {
                    using var sr = new StreamReader(stream, bufferSize: 81920); // Default :1024 byte
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(sr);
                    xrefMap.BaseUrl = ResolveBaseUrl(xrefMap, uri);
                    return xrefMap;
                }
        }
    }

    private static string ResolveBaseUrl(XRefMap map, Uri uri)
    {
        if (!string.IsNullOrEmpty(map.BaseUrl))
            return map.BaseUrl;

        // If downloaded xrefmap has no baseUrl.
        // Use xrefmap file download url as basePath.
        var baseUrl = uri.GetLeftPart(UriPartial.Path);
        return baseUrl.Substring(0, baseUrl.LastIndexOf('/') + 1);
    }
}
