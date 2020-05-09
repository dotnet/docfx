// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            Func<List<Error>> getErrors,
            Func<SourceInfo<string>, string> getLink,
            Func<SourceInfo<string>?, SourceInfo<string>?, bool, (string? href, string display)> resolveXref)
        {
            return builder.Use(document =>
            {
                var errors = getErrors();
                var file = InclusionContext.File as Document;
                var scanTags = file != null && file.ContentType == ContentType.Page && TemplateEngine.IsConceptual(file.Mime);

                document.Visit(node =>
                {
                    switch (node)
                    {
                        case TabTitleBlock _:
                            return true;
                        case HtmlBlock block:
                            block.Lines = new StringLineGroup(ProcessHtml(block.Lines.ToString(), block, errors, scanTags));
                            return false;
                        case HtmlInline inline:
                            inline.Tag = ProcessHtml(inline.Tag, inline, errors, scanTags);
                            return false;
                        default:
                            return false;
                    }
                });
            });

            string ProcessHtml(string html, MarkdownObject block, List<Error> errors, bool scanTags)
            {
                // <a>b</a> generates 3 inline markdown tokens: <a>, b, </a>.
                // `HtmlNode.OuterHtml` turns <a> into <a></a>, and generates <a></a>b</a> for the above input.
                // The following code ensures we preserve the original html when changing links.
                return HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlToken token) =>
                {
                    HtmlUtility.TransformLink(ref token, block, getLink);
                    HtmlUtility.TransformXref(ref reader, ref token, block, resolveXref);

                    if (scanTags)
                    {
                        HtmlUtility.ScanTags(ref token, block, errors);
                    }

                    HtmlUtility.RemoveRerunCodepenIframes(ref token);
                    HtmlUtility.StripTags(ref reader, ref token);
                });
            }
        }
    }
}
