// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class TabContentBlock : ContainerBlock
{
    public string Id { get; }

    public TabContentBlock(List<Block> blocks, string id = null)
        : base(null)
    {
        Id = id;
        foreach (var item in blocks)
        {
            Add(item);
        }
    }
}
