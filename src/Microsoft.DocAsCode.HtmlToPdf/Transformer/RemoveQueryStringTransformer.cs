// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HtmlAgilityPack;

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.HtmlToPdf.Transformer;

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
                            var resolvedUrl = src.RemoveUrlQueryString();

                            if (src != resolvedUrl)
                            {
                                tag.Attributes["href"].Value = string.IsNullOrEmpty(resolvedUrl) ? "#" : resolvedUrl;
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
