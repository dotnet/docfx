// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Markdig;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitleExtension
    {
        public static MarkdownPipelineBuilder UseExtractTitle(
            this MarkdownPipelineBuilder builder, MarkdownEngine markdownEngine, Action<SourceInfo<string?>> setTitle, Action<string> setRawTitle)
        {
            return builder.Use(document =>
            {
                var hasVisibleNodes = false;

                document.Replace(obj =>
                {
                    switch (obj)
                    {
                        case HeadingBlock heading when heading.Level == 1 || heading.Level == 2 || heading.Level == 3:
                            if (heading.Inline.Any())
                            {
                                setTitle(new SourceInfo<string?>(markdownEngine.ToPlainText(heading), heading.GetSourceInfo()));
                            }

                            if (!hasVisibleNodes)
                            {
                                setRawTitle(markdownEngine.ToHtml(heading));
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
