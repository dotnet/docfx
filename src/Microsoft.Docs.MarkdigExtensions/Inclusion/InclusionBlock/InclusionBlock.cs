// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class InclusionBlock : ContainerBlock
{
    public string Title { get; set; }

    public string IncludedFilePath { get; set; }

    public string GetRawToken() => $"[!include[{Title}]({IncludedFilePath})]";

    public InclusionBlock(BlockParser parser)
        : base(parser)
    {
    }
}
