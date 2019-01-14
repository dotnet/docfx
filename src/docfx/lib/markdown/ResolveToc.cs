// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Markdig;
using Markdig.Parsers;

namespace Microsoft.Docs.Build
{
    internal static class ResolveToc
    {
        public static MarkdownPipelineBuilder UseTocHeading(this MarkdownPipelineBuilder builder, int maxLeadingCount = 20)
        {
            Debug.Assert(maxLeadingCount > 0);

            var headingBlockParser = builder.BlockParsers.Find<HeadingBlockParser>();

            if (headingBlockParser != null)
            {
                headingBlockParser.MaxLeadingCount = maxLeadingCount;
            }

            return builder;
        }
    }
}
