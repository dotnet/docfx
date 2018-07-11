// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitle
    {
        public static MarkdownPipelineBuilder UseExtractTitle(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                var h1 = document.SkipWhile(block => block is YamlFrontMatterBlock).FirstOrDefault() as HeadingBlock;

                if (h1 != null && h1.Level == 1)
                {
                    Markup.Context.TitleHtml = RenderTitle(h1);
                    document.Remove(h1);
                }
            });
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
