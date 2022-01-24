// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers.Html;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class MonikerZoneExtension
{
    public static MarkdownPipelineBuilder UseMonikerZone(this MarkdownPipelineBuilder builder, Func<SourceInfo<string?>, MonikerList> parseMonikerRange)
    {
        return builder.Use(document => document.Replace(node =>
        {
            if (node is MonikerRangeBlock monikerRangeBlock)
            {
                var monikers = parseMonikerRange(new(monikerRangeBlock.MonikerRange, monikerRangeBlock.GetSourceInfo()));
                if (!monikers.HasMonikers)
                {
                    return null;
                }

                monikerRangeBlock.ParsedMonikers = monikers;
                monikerRangeBlock.GetAttributes().Properties?.Remove(new("range", monikerRangeBlock.MonikerRange));
                monikerRangeBlock.GetAttributes().AddPropertyIfNotExist("data-moniker", string.Join(" ", monikers));
            }
            return node;
        }));
    }
}
