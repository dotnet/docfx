// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;

    public class XRefArchiveBuilder
    {
        private const string PhaseName = "Archive";

        private readonly object _syncRoot = new object();
        private readonly XRefMapDownloader _downloader = new XRefMapDownloader();

        public async Task<bool> DownloadAsync(Uri uri, string outputFile)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("Relative path is not allowed.", nameof(uri));
            }
            using (new LoggerPhaseScope(PhaseName))
            using (new LoggerFileScope(outputFile))
            {
                Logger.LogInfo($"Creating xref archive file...");
                try
                {
                    using (var xa = XRefArchive.Open(outputFile, XRefArchiveMode.Create))
                    {
                        await DownloadCoreAsync(uri, xa, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unable to create archive: {ex.Message}");
                    return false;
                }
                Logger.LogInfo($"Xref archive file created.");
                return true;
            }
        }

        private async Task<string> DownloadCoreAsync(Uri uri, XRefArchive xa, bool isMajor)
        {
            IXRefContainer container;
            container = await _downloader.DownloadAsync(uri);
            var map = container as XRefMap;
            if (map == null)
            {
                // not support download an xref archive, or reference to an xref archive
                return null;
            }
            if (map.Redirections?.Count > 0)
            {
                await RewriteRedirections(uri, xa, map);
            }
            if (map.References?.Count > 0 && map.HrefUpdated != true)
            {
                if (string.IsNullOrEmpty(map.BaseUrl))
                {
                    XRefMapDownloader.UpdateHref(map, uri);
                }
            }
            lock (_syncRoot)
            {
                if (isMajor)
                {
                    return xa.CreateMajor(map);
                }
                else
                {
                    return xa.CreateMinor(map, GetNames(uri, map));
                }
            }
        }

        private static IEnumerable<string> GetNames(Uri uri, XRefMap map)
        {
            var name = uri.Segments.LastOrDefault();
            yield return name;
            if (map.References?.Count > 0)
            {
                yield return map.References[0].Uid;
            }
        }

        #region Rewrite redirections

        private async Task<List<XRefMapRedirection>> RewriteRedirections(Uri uri, XRefArchive xa, XRefMap map) =>
            (from list in
                await Task.WhenAll(
                    from r in map.Redirections
                    where !string.IsNullOrEmpty(r.Href)
                    group r by r.Href into g
                    let href = GetHrefUri(uri, g.Key)
                    where href != null
                    select RewriteRedirectionsCore(g.ToList(), href, xa))
             from r in list
             orderby (r.UidPrefix ?? string.Empty).Length descending, (r.UidPrefix ?? string.Empty)
             select r).ToList();

        private async Task<List<XRefMapRedirection>> RewriteRedirectionsCore(List<XRefMapRedirection> redirections, Uri uri, XRefArchive xa)
        {
            var fileRef = await DownloadCoreAsync(uri, xa, false);
            if (fileRef == null)
            {
                return new List<XRefMapRedirection>();
            }
            return (from r in redirections
                    select new XRefMapRedirection { UidPrefix = r.UidPrefix, Href = fileRef }).ToList();
        }

        private static Uri GetHrefUri(Uri uri, string href)
        {
            if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out Uri hrefUri))
            {
                Logger.LogWarning($"Invalid redirection href: {href}.");
                return null;
            }
            if (!hrefUri.IsAbsoluteUri)
            {
                hrefUri = new Uri(uri, hrefUri);
            }
            return hrefUri;
        }

        #endregion
    }
}
