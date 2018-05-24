// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitle
    {
        public static MarkdownPipelineBuilder UseExtractTitle(this MarkdownPipelineBuilder builder, StrongBox<string> result)
        {
            return builder.Use(document =>
            {
                var h1 = document.SkipWhile(block => block is YamlFrontMatterBlock).FirstOrDefault() as HeadingBlock;

                if (h1 != null && h1.Level == 1)
                {
                    // TODO: H1 with markdown formats?
                    result.Value = h1.Inline.FirstChild.ToString();
                    document.Remove(h1);
                }
            });
        }
    }
}
