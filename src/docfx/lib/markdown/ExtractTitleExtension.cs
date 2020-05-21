// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitleExtension
    {
        public static MarkdownPipelineBuilder UseExtractTitle(this MarkdownPipelineBuilder builder, Func<ConceptualModel?> getConceptual)
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
                            if (conceptual.Title is null && heading.Inline.Any())
                            {
                                conceptual.Title = heading.ToPlainText();
                            }

                            if (!hasVisibleNodes)
                            {
                                conceptual.RawTitle = heading.ToHtml();
                                conceptual.RawTitleId = heading.TryGetAttributes()?.Id;
                                hasVisibleNodes = true;
                                return null;
                            }
                            return obj;

                        case LeafBlock _ when obj.IsVisible():
                            hasVisibleNodes = true;
                            return obj;

                        default:
                            return obj;
                    }
                });
            });
        }
    }
}
