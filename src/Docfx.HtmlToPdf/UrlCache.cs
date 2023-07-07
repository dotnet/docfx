// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.HtmlToPdf;

public class UrlCache
{
    private readonly Dictionary<string, ManifestItem> _cache;

    public UrlCache(string basePath, ManifestItem[] manifestItemWithAssetIds)
    {
        Guard.ArgumentNotNullOrEmpty(basePath, nameof(basePath));
        Guard.ArgumentNotNull(manifestItemWithAssetIds, nameof(manifestItemWithAssetIds));

        _cache = new Dictionary<string, ManifestItem>();
        foreach (var item in manifestItemWithAssetIds)
        {
            string relativePath = ManifestUtility.GetRelativePath(item, OutputType.Html);
            if (!string.IsNullOrEmpty(relativePath) && relativePath.EndsWith(BuildToolConstants.OutputFileExtensions.ContentHtmlExtension, StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = PdfHelper.NormalizeFileLocalPath(basePath, relativePath);
                if (!string.IsNullOrEmpty(fullPath) && !_cache.ContainsKey(fullPath))
                {
                    _cache.Add(fullPath, item);
                }
            }
        }
    }

    public UrlCache(string basePath, IEnumerable<string> items)
    {
        Guard.ArgumentNotNullOrEmpty(basePath, nameof(basePath));
        Guard.ArgumentNotNull(items, nameof(items));

        _cache = new Dictionary<string, ManifestItem>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item))
            {
                var fullPath = PdfHelper.NormalizeFileLocalPath(basePath, item);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath) && !_cache.ContainsKey(fullPath))
                {
                    _cache.Add(fullPath, null);
                }
            }
        }
    }

    public ManifestItem Query(string url)
    {
        if (_cache.TryGetValue(url, out ManifestItem manifestItemWithAssetId))
        {
            return manifestItemWithAssetId;
        }
        return null;
    }

    public bool Contains(string url)
    {
        return _cache.ContainsKey(url);
    }
}
