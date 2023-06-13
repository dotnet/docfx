// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.Build.Engine;

public class XRefMapDownloader
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
    public async Task<IXRefContainer> DownloadAsync(Uri uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }
        await _semaphore.WaitAsync();
        return await Task.Run(async () =>
        {
            try
            {
                if (uri.IsAbsoluteUri)
                {
                    return await DownloadBySchemeAsync(uri);
                }
                else
                {
                    return ReadLocalFileWithFallback(uri);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    private IXRefContainer ReadLocalFileWithFallback(Uri uri)
    {
        foreach (var localFileFolder in _localFileFolders)
        {
            var localFilePath = Path.Combine(localFileFolder, uri.OriginalString);
            if (File.Exists(localFilePath))
            {
                return ReadLocalFile(localFilePath);
            }
        }
        throw new FileNotFoundException($"Cannot find xref map file {uri.OriginalString} in path: {string.Join(",", _localFileFolders)}", uri.OriginalString);
    }

    /// <remarks>
    /// Support scheme: http, https, ftp, file, embedded.
    /// </remarks>
    protected virtual async Task<IXRefContainer> DownloadBySchemeAsync(Uri uri)
    {
        IXRefContainer result = null;
        if (uri.IsFile)
        {
            result = DownloadFromLocal(uri);
        }
        else if (uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps ||
            uri.Scheme == Uri.UriSchemeFtp)
        {
            result = await DownloadFromWebAsync(uri);
        }
        else if (uri.Scheme == "embedded")
        {
            result = DownloadFromAssembly(uri);
        }
        else
        {
            throw new ArgumentException($"Unsupported scheme {uri.Scheme}, expected: http, https, ftp, file, embedded.", nameof(uri));
        }
        if (result == null)
        {
            throw new InvalidDataException($"Invalid yaml file from {uri}.");
        }
        return result;
    }

    protected static IXRefContainer DownloadFromLocal(Uri uri)
    {
        var filePath = uri.LocalPath;
        return ReadLocalFile(filePath);
    }

    private static IXRefContainer ReadLocalFile(string filePath)
    {
        if (".zip".Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase))
        {
            return XRefArchive.Open(filePath, XRefArchiveMode.Read);
        }

        using var sr = File.OpenText(filePath);
        return YamlUtility.Deserialize<XRefMap>(sr);
    }

    protected static async Task<XRefMap> DownloadFromWebAsync(Uri uri)
    {
        var baseUrl = uri.GetLeftPart(UriPartial.Path);
        baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/') + 1);

        using var httpClient = new HttpClient(new HttpClientHandler() { CheckCertificateRevocationList = true });
        using var stream = await httpClient.GetStreamAsync(uri);
        using var sr = new StreamReader(stream);
        var map = YamlUtility.Deserialize<XRefMap>(sr);
        map.BaseUrl = baseUrl;
        UpdateHref(map, null);
        return map;
    }

    private XRefMap DownloadFromAssembly(Uri uri)
    {
        var path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        var index = path.IndexOf('/');
        if (index == -1)
        {
            throw new ArgumentException($"Invalid uri {uri.OriginalString}, expect: {uri.Scheme}:{{assemblyName}}/{{resourceName}}", nameof(uri));
        }
        var assemblyName = path.Remove(index);
        var resourceName = assemblyName + "." + path.Substring(index + 1);

        var assembly = AppDomain.CurrentDomain.Load(assemblyName);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var sr = new StreamReader(stream);
        return YamlUtility.Deserialize<XRefMap>(sr);
    }

    public static void UpdateHref(XRefMap map, Uri uri)
    {
        if (!string.IsNullOrEmpty(map.BaseUrl))
        {
            if (!Uri.TryCreate(map.BaseUrl, UriKind.Absolute, out Uri baseUri))
            {
                throw new InvalidDataException($"Xref map file (from {uri.AbsoluteUri}) has an invalid base url: {map.BaseUrl}.");
            }
            map.UpdateHref(baseUri);
            return;
        }
        if (uri.Scheme == "http" || uri.Scheme == "https")
        {
            map.UpdateHref(uri);
            return;
        }
        throw new InvalidDataException($"Xref map file (from {uri.AbsoluteUri}) missing base url.");
    }
}
