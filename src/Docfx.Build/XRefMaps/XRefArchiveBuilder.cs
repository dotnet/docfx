// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Build.Engine;

public class XRefArchiveBuilder
{
    private readonly object _syncRoot = new();
    private readonly XRefMapDownloader _downloader = new();

    public async Task<bool> DownloadAsync(Uri uri, string outputFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("Relative path is not allowed.", nameof(uri));
        }
        using (new LoggerFileScope(outputFile))
        {
            Logger.LogInfo("Creating xref archive file...");
            try
            {
                using var xa = XRefArchive.Open(outputFile, XRefArchiveMode.Create);
                await DownloadCoreAsync(uri, xa, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unable to create archive: {ex.Message}");
                return false;
            }
            Logger.LogInfo("Xref archive file created.");
            return true;
        }
    }

    private async Task<string> DownloadCoreAsync(Uri uri, XRefArchive xa, CancellationToken cancellationToken)
    {
        IXRefContainer container;
        container = await _downloader.DownloadAsync(uri, cancellationToken);
        if (container is not XRefMap map)
        {
            // XRefArchive is not supported by `docfx download`.
            Logger.LogWarning($"Download an xref archive, or reference to an xref archive is not supported. URI: {uri}");
            return null;
        }

        // Enforce XRefMap's references are sorted by uid.
        // Note:
        //   Sort is not needed if `map.Sorted == true`.
        //   But there are some xrefmap files that is not propery sorted by using InvariantCulture.
        //   (e.g. Unity xrefmap that maintained by community)
        if (map.References is { Count: > 0 })
        {
            map.References.Sort(XRefSpecUidComparer.Instance);
            map.Sorted = true;
        }

        // Write XRefMap content to `xrefmap.yml`.
        lock (_syncRoot)
        {
            return xa.CreateMajor(map);
        }
    }
}
