// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlReaderWriter;
using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class HtmlExtension
{
    public static MarkdownPipelineBuilder UseHtml(
        this MarkdownPipelineBuilder builder,
        Func<ErrorBuilder> getErrors,
        Func<LinkInfo, string> getLink,
        Func<SourceInfo<string>?, SourceInfo<string>?, bool, XrefLink> resolveXref,
        HtmlSanitizer htmlSanitizer,
        DocumentProvider documentProvider)
    {
        return builder.Use(document =>
        {
            var errors = getErrors();
            var file = ((SourceInfo)InclusionContext.File).File;
            var rootFile = ((SourceInfo)InclusionContext.RootFile).File;
            var elementCount = new Dictionary<string, Dictionary<string, int>>();

            document.Visit(node =>
            {
                switch (node)
                {
                    case TabTitleBlock:
                        return true;
                    case HtmlBlock block:
                        block.Lines = new StringLineGroup(ProcessHtml(block.Lines.ToString().Replace("\r", ""), block, errors, elementCount));
                        return false;
                    case HtmlInline inline:
                        inline.Tag = ProcessHtml(inline.Tag, inline, errors, elementCount);
                        return false;
                    default:
                        return false;
                }
            });

            Telemetry.TrackHtmlElement(
                file,
                documentProvider.GetContentType(rootFile),
                documentProvider.GetMime(rootFile),
                elementCount,
                (tagName, attributeName) => htmlSanitizer.IsAllowedHtml(tagName, attributeName));
        });

        string ProcessHtml(
            string html,
            MarkdownObject block,
            ErrorBuilder errors,
            Dictionary<string, Dictionary<string, int>> elementCount)
        {
            HtmlUtility.CollectHtmlUsage(html, elementCount);

            // <a>b</a> generates 3 inline markdown tokens: <a>, b, </a>.
            // `HtmlNode.OuterHtml` turns <a> into <a></a>, and generates <a></a>b</a> for the above input.
            // The following code ensures we preserve the original html when changing links.
            return HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.TransformLink(ref token, block, getLink!);
                HtmlUtility.TransformXref(ref reader, ref token, block, resolveXref);
                HtmlUtility.RemoveRerunCodepenIframes(ref token);
                htmlSanitizer.SanitizeHtml(errors, ref reader, ref token, block);
            });
        }
    }
}
