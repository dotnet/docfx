// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Converts markdown to html
    /// </summary>
    internal static class MarkdownUtility
    {
        public static (string html, JObject metadata) Markup(string markdown, Document file, Context context)
        {
            using (InclusionContext.PushFile(file))
            {
                var pipeline = CreateMarkdownPipeline();
                var html = Markdown.ToHtml(markdown, pipeline);
                return (html, null);
            }
        }

        private static MarkdownPipeline CreateMarkdownPipeline(Context context)
        {
            var markdownContext = new MarkdownContext(
                null,
                context.ReportWarning,
                context.ReportError,
                null, null, null);

            return new MarkdownPipelineBuilder()
                .UseDocfxExtensions(markdownContext)
                .Build();
        }
    }
}
