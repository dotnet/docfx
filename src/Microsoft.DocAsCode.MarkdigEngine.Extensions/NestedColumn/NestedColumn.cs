// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class NestedColumnBlock : ContainerBlock
{
    public NestedColumnBlock(BlockParser parser) : base(parser)
    {
    }

    public int ColonCount { get; set; }

    public string ColumnWidth { get; set; }
}
