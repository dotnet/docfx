// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf.Transformer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;

    public class RemoveQueryStringTransformer : ITransformer
    {
        public void Transform(IEnumerable<string> htmlFilePaths)
        {
            Guard.ArgumentNotNull(htmlFilePaths, nameof(htmlFilePaths));

            Parallel.ForEach(
                htmlFilePaths,
                htmlFilePath =>
                {
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
                                var resovedUrl = src.RemoveUrlQueryString();

                                if (src != resovedUrl)
                                {
                                    tag.Attributes["href"].Value = string.IsNullOrEmpty(resovedUrl) ? "#" : resovedUrl;
                                    isTransformed = true;
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
                        Logger.LogWarning($"Removing url query string error, details: {ex.Message}", htmlFilePath);
                    }
                });
        }
    }
}