// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Syntax;

namespace Microsoft.Docs.Build;

internal static class ExtractTitleExtension
{
    public static MarkdownPipelineBuilder UseExtractTitle(
        this MarkdownPipelineBuilder builder, MarkdownEngine markdownEngine, Func<ConceptualModel?> getConceptual)
    {
        return builder.Use(document =>
        {
            var hasVisibleNodes = false;
            var conceptual = getConceptual();
            if (conceptual is null)
            {
                return;
            }

            document.Replace(obj =>
            {
                switch (obj)
                {
                    case HeadingBlock heading when heading.Level == 1 || heading.Level == 2 || heading.Level == 3:
                        if (conceptual.Title.Value is null && heading.Inline != null && heading.Inline.Any())
                        {
                            conceptual.Title = new SourceInfo<string?>(markdownEngine.ToPlainText(heading), heading.GetSourceInfo());
                        }

                        if (!hasVisibleNodes)
                        {
                            conceptual.RawTitle = markdownEngine.ToHtml(heading);
                            hasVisibleNodes = true;
                            return null;
                        }
                        return obj;

                    case LeafBlock when obj.IsVisible():
                        hasVisibleNodes = true;
                        return obj;

                    default:
                        return obj;
                }
            });
        });
    }
}
