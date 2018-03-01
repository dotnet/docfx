// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf.Transformer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;

    public class FrameTransformer : ITransformer
    {
        private static readonly string[] DefaultReplaceHosts = new string[] { "hubs-video.ssl.catalog.video.msn.com" };
        private readonly string[] _replaceHosts;

        public FrameTransformer(params string[] replaceHosts)
        {
            _replaceHosts = replaceHosts ?? DefaultReplaceHosts;
        }

        public void Transform(IEnumerable<string> htmlFilePaths)
        {
            if (_replaceHosts.Length == 0)
            {
                return;
            }

            Parallel.ForEach(
                htmlFilePaths,
                htmlFilePath =>
            {
                try
                {
                    var doc = new HtmlDocument();
                    doc.Load(htmlFilePath);
                    var iframes = doc.DocumentNode.SelectNodes("//iframe[@src]");
                    if (iframes != null && iframes.Count > 0)
                    {
                        bool isTransformed = false;
                        foreach (var iframe in iframes)
                        {
                            var src = iframe.Attributes["src"].Value;
                            if (Uri.TryCreate(src, UriKind.Absolute, out Uri uri))
                            {
                                string host = uri.Host;
                                if (_replaceHosts.Any(p => string.Equals(p, host, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var newNode = HtmlNode.CreateNode($"<a href='{src}'>Click here to view</a>");
                                    iframe.ParentNode.ReplaceChild(newNode, iframe);
                                    isTransformed = true;
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
                    Logger.LogWarning($"Transfer frame error, details: {ex.Message}", htmlFilePath);
                }
            });
        }
    }
}
