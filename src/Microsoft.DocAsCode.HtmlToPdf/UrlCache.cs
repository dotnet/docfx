// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class UrlCache
    {
        private readonly Dictionary<string, ManifestItemWithAssetId> _cache;

        public UrlCache(string basePath, ManifestItemWithAssetId[] manifestItemWithAssetIds)
        {
            Guard.ArgumentNotNullOrEmpty(basePath, nameof(basePath));
            Guard.ArgumentNotNull(manifestItemWithAssetIds, nameof(manifestItemWithAssetIds));

            _cache = new Dictionary<string, ManifestItemWithAssetId>();
            foreach (var item in manifestItemWithAssetIds)
            {
                string relativePath = item.Output?.Html?.RelativePath;
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

            _cache = new Dictionary<string, ManifestItemWithAssetId>();
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

        public ManifestItemWithAssetId Query(string url)
        {
            ManifestItemWithAssetId manifestItemWithAssetId;
            if (_cache.TryGetValue(url, out manifestItemWithAssetId))
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
}
