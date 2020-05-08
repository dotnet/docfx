// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class LinkExtension
    {
        public static MarkdownPipelineBuilder UseLink(
            this MarkdownPipelineBuilder builder, Func<SourceInfo<string>, string> getLink)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (node is TabTitleBlock)
                    {
                        return true;
                    }
                    else if (node is LinkInline link)
                    {
                        var href = new SourceInfo<string>(link.Url, link.ToSourceInfo());
                        link.Url = getLink(href);
                    }
                    else if (node is TripleColonBlock tripleColonBlock && tripleColonBlock.Extension is ImageExtension)
                    {
                        var blockProperties = tripleColonBlock.GetAttributes().Properties;
                        for (var i = 0; i < blockProperties.Count; i++)
                        {
                            if (blockProperties[i].Key == "src")
                            {
                                var href = new SourceInfo<string>(blockProperties[i].Value, tripleColonBlock.ToSourceInfo());
                                blockProperties[i] = new KeyValuePair<string, string>("src", getLink(href) ?? href);
                                break;
                            }
                        }
                    }
                    return false;
                });
            });
        }
    }
}
