// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf.Transformer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;

    public class HtmlNotInTocTransformer : ITransformer
    {
        private readonly string _basePath;
        private readonly UrlCache _manifestUrlCache;
        private readonly PdfOptions _pdfOptions;

        public HtmlNotInTocTransformer(string basePath, UrlCache manifestUrlCache, PdfOptions pdfOptions)
        {
            Guard.ArgumentNotNullOrEmpty(basePath, nameof(basePath));
            Guard.ArgumentNotNull(manifestUrlCache, nameof(manifestUrlCache));
            Guard.ArgumentNotNull(pdfOptions, nameof(pdfOptions));
            _basePath = basePath;
            _manifestUrlCache = manifestUrlCache;
            _pdfOptions = pdfOptions;
        }

        /// <summary>
        /// 1. Retrive <a href=''></a> from each html.
        /// 2. Foreach link, try to fix it.
        ///    2.1 If the link is full path, just keep it.
        ///    2.2 If the link is root path(/a/b), just add host in prefix.
        ///    2.3 If the link is relative path(a/b.html)
        ///        2.3.1 If the link in TOC, just keep it.
        ///        2.3.2 If the link NOT in TOC and in Manifest, try to get the canonical url.
        ///        2.3.3 If the link NOT in TOC and NOT in Manifest, log warning to the invalid link.
        ///    2.4 Others, keep it as the origin.
        /// </summary>
        /// <param name="htmlFilePaths">The htmls' relative path in TOC.</param>
        public void Transform(IEnumerable<string> htmlFilePaths)
        {
            Guard.ArgumentNotNull(htmlFilePaths, nameof(htmlFilePaths));

            var tocUrlCache = new UrlCache(_basePath, htmlFilePaths);
            Parallel.ForEach(
                htmlFilePaths,
                htmlFilePath =>
            {
                var doc = new HtmlDocument();
                var currentHtml = PdfHelper.NormalizeFileLocalPath(_basePath, htmlFilePath, false);
                if (!File.Exists(currentHtml))
                {
                    return;
                }

                try
                {
                    var baseDirectory = Path.GetDirectoryName(currentHtml);
                    doc.Load(currentHtml);
                    var tags = doc.DocumentNode.SelectNodes("//a[@href]");
                    if (tags?.Count > 0)
                    {
                        bool isTransformed = false;
                        foreach (var tag in tags)
                        {
                            var src = tag.Attributes["href"].Value;
                            if (Uri.TryCreate(src, UriKind.Relative, out Uri uri))
                            {
                                try
                                {
                                    if (Path.IsPathRooted(src))
                                    {
                                        if (string.IsNullOrEmpty(_pdfOptions.Host))
                                        {
                                            Logger.LogVerbose($"No host passed, so just keep the url as origin: {src}.", htmlFilePath);
                                            continue;
                                        }
                                        if (Uri.TryCreate(_pdfOptions.Host, UriKind.Absolute, out Uri host))
                                        {
                                            tag.Attributes["href"].Value = new Uri(host, uri.OriginalString).ToString();
                                            isTransformed = true;
                                        }
                                        else
                                        {
                                            Logger.LogVerbose($"The host format:{_pdfOptions.Host} is invalid, so just keep the url as origin: {src}.", htmlFilePath);
                                        }
                                    }
                                    else
                                    {
                                        // uri.OriginalString may be "virtual-machines-windows.html#abc?toc=%2fazure%2fvirtual-machines%2fwindows%2ftoc.json",then we cannot find the file, so just remove the querystring.
                                        var withoutQueryString = uri.OriginalString.RemoveUrlQueryString();
                                        if (uri.OriginalString != withoutQueryString)
                                        {
                                            tag.Attributes["href"].Value = withoutQueryString;
                                            isTransformed = true;
                                        }

                                        // originalString is used to find the file, so need to remove querystring and bookmark
                                        var originalString = uri.OriginalString.RemoveUrlQueryString().RemoveUrlBookmark();

                                        // because when publish to website the url path is toLower so use OrdinalIgnoreCase.
                                        string srcInCurrentHtml = new Uri(Path.Combine(baseDirectory, originalString)).LocalPath?.ToLower();
                                        if (originalString.EndsWith(BuildToolConstants.OutputFileExtensions.ContentHtmlExtension, StringComparison.OrdinalIgnoreCase) && !tocUrlCache.Contains(srcInCurrentHtml))
                                        {
                                            var conceptual = _manifestUrlCache.Query(srcInCurrentHtml);
                                            var assetId = ManifestUtility.GetAssetId(conceptual);
                                            if (conceptual == null)
                                            {
                                                Logger.LogWarning($"Can not find the relative path: {uri.OriginalString} in manifest. So skip to fix the invalid link.", htmlFilePath);
                                                continue;
                                            }

                                            if (!string.IsNullOrEmpty(_pdfOptions.Locale) && !string.IsNullOrEmpty(_pdfOptions.Host))
                                            {
                                                // the assetId may has '.html' extension, but we should redirect to the site which should not have '.html' extension, so trim it here.
                                                tag.Attributes["href"].Value = string.Format(_pdfOptions.ExternalLinkFormat, assetId.TrimEnd(BuildToolConstants.OutputFileExtensions.ContentHtmlExtension));
                                                isTransformed = true;
                                            }
                                            else
                                            {
                                                Logger.LogVerbose($"Host/Locale is null or empty, so just skip to keep the it as origin: {uri.OriginalString}.", htmlFilePath);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogWarning(ex.Message, htmlFilePath);
                                }
                            }
                        }
                        if (isTransformed)
                        {
                            doc.Save(currentHtml);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Transfer html not in toc error, details: {ex.Message}", htmlFilePath);
                }
            });
        }
    }
}
