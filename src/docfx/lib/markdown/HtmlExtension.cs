// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class HtmlExtension
    {
        public static MarkdownPipelineBuilder UseHtml(
            this MarkdownPipelineBuilder builder,
            Func<SourceInfo<string>, string> getLink,
            Func<SourceInfo<string>?, SourceInfo<string>?, bool, (string? href, string display)> resolveXref)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    switch (node)
                    {
                        case TabTitleBlock _:
                            return true;
                        case HtmlBlock block:
                            block.Lines = new StringLineGroup(ProcessHtml(block.Lines.ToString(), block));
                            return false;
                        case HtmlInline inline:
                            inline.Tag = ProcessHtml(inline.Tag, inline);
                            return false;
                        default:
                            return false;
                    }
                });
            });

            string ProcessHtml(string html, MarkdownObject block)
            {
                // <a>b</a> generates 3 inline markdown tokens: <a>, b, </a>.
                // `HtmlNode.OuterHtml` turns <a> into <a></a>, and generates <a></a>b</a> for the above input.
                // The following code ensures we preserve the original html when changing links.
                return HtmlUtility.TransformHtml(html, (ref HtmlToken token) =>
                {
                    HtmlUtility.TransformLink(ref token, block, getLink);
                    HtmlUtility.TransformXref(ref token, block, resolveXref);
                    HtmlUtility.RemoveRerunCodepenIframes(ref token);
                });
            }
        }
    }
}
