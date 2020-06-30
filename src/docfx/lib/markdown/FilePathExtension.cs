// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class FilePathExtension
    {
        public static MarkdownPipelineBuilder UseFilePath(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                var file = (Document)InclusionContext.File;

                document.Visit(obj =>
                {
                    switch (obj)
                    {
                        case Block block when block.Parent is null:
                            block.SetFilePath(file);
                            break;

                        case Inline inline when inline.Parent is null:
                            inline.SetFilePath(file);
                            break;
                    }
                    return false;
                });
            });
        }
    }
}
