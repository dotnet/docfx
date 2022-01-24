// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class RowBlock : ContainerBlock
{
    public int ColonCount { get; set; }

    public RowBlock(BlockParser parser)
        : base(parser)
    {
    }
}
