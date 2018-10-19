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

    public class AbsolutePathInTocPageFileTransformer : ITransformer
    {
        private readonly PdfOptions _pdfOptions;

        public AbsolutePathInTocPageFileTransformer(PdfOptions pdfOptions)
        {
            Guard.ArgumentNotNull(pdfOptions, nameof(pdfOptions));
            _pdfOptions = pdfOptions;
        }

        public void Transform(IEnumerable<string> htmlFilePaths)
        {
            Guard.ArgumentNotNull(htmlFilePaths, nameof(htmlFilePaths));
            Parallel.ForEach(
                htmlFilePaths,
                htmlFilePath =>
                {
                    if (!File.Exists(htmlFilePath))
                    {
                        Logger.LogVerbose($"Can not find toc page file: {htmlFilePath}.", htmlFilePath);
                        return;
                    }

                    try
                    {
                        var doc = new HtmlDocument();
                        doc.Load(htmlFilePath);
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
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogWarning(ex.Message, htmlFilePath);
                                    }
                                }
                            }
                            if (isTransformed)
                            {
                                doc.Save(htmlFilePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Transfer absolute path in toc page file error, details: {ex.Message}", htmlFilePath);
                    }
                });
        }
    }
}
