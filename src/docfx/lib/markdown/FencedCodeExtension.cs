// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Parsers;

namespace Microsoft.Docs.Build;

internal static class FencedCodeExtension
{
    public static MarkdownPipelineBuilder UseFencedCodeLangPrefix(this MarkdownPipelineBuilder builder)
    {
        return builder.Use(pipeline =>
        {
            if (pipeline.BlockParsers.FindExact<FencedCodeBlockParser>() is FencedCodeBlockParser parser)
            {
                parser.InfoPrefix = "lang-";
            }
        });
    }
}
