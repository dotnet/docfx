// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitle
    {
        public static MarkdownPipelineBuilder UseExtractTitle(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                if (InclusionContext.IsInclude
                    && (Markup.Result.HasTitle || !Markup.Result.FirstBlockIsInclusionBlock))
                {
                    return;
                }

                var firstBlock = GetFirstVisibleBlock(document);
                var heading = firstBlock as HeadingBlock;

                if (heading != null && heading.Level == 1)
                {
                    Markup.Result.HtmlTitle = RenderTitle(heading);
                    document.Remove(heading);
                }
                Markup.Result.FirstBlockIsInclusionBlock = firstBlock is InclusionBlock;
            });
        }

        private static Block GetFirstVisibleBlock(MarkdownDocument document)
        {
            return GetAfterYamlChildren(document).SkipWhile(IsCommentsBlock).FirstOrDefault();
        }

        private static IEnumerable<Block> GetAfterYamlChildren(MarkdownDocument document)
        {
            return document.FirstOrDefault() is YamlFrontMatterBlock ? document.Skip(1) : document;
        }

        private static bool IsCommentsBlock(Block block)
        {
            var htmlBlock = block as HtmlBlock;

            return htmlBlock?.Type == HtmlBlockType.Comment;
        }

        private static string RenderTitle(HeadingBlock h1)
        {
            var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.Render(h1);

            return writer.ToString().Trim();
        }
    }
}
