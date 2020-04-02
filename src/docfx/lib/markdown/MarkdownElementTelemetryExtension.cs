// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownElementTelemetryExtension
    {
        public static MarkdownPipelineBuilder UseMarkdownElementTelemetry(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    Telemetry.TrackMarkdownElement((Document)InclusionContext.File, MarkdigUtility.GetElementType(node), MarkdigUtility.GetTokenType(node));
                    return false;
                });
            });
        }
    }
}
