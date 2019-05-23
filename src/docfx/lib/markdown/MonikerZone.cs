// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Markdig;
using Markdig.Renderers.Html;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MonikerZone
    {
        public static MarkdownPipelineBuilder UseMonikerZone(this MarkdownPipelineBuilder builder, Func<SourceInfo<string>, List<string>> parseMonikerRange)
        {
            return builder.Use(document =>
            {
                document.Replace(node =>
                {
                    if (node is MonikerRangeBlock monikerRangeBlock)
                    {
                        monikerRangeBlock.GetAttributes().Properties.Remove(new KeyValuePair<string, string>("range", monikerRangeBlock.MonikerRange));
                        monikerRangeBlock.GetAttributes().AddPropertyIfNotExist("data-moniker", string.Join(
                            " ",
                            parseMonikerRange(new SourceInfo<string>(monikerRangeBlock.MonikerRange, monikerRangeBlock.ToSourceInfo()))));
                    }
                    return node;
                });
            });
        }
    }
}
