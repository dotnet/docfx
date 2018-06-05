// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.Build
{
    internal static class ResolveHtmlLink
    {
        public static MarkdownPipelineBuilder UseResolveHtmlLink(this MarkdownPipelineBuilder builder, Context context)
        {
            builder.Use((HtmlRenderer renderer, HtmlBlock block) =>
            {

            });

            builder.Use((HtmlRenderer renderer, HtmlInline block) =>
            {

            });

            return builder;
        }
    }
}
