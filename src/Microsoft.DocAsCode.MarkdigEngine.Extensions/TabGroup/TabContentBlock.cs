// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    public class TabContentBlock : ContainerBlock
    {
        public TabTitleBlock TabTitle { get; }

        public TabContentBlock(List<Block> blocks, TabTitleBlock tabTitle)
            : base(null)
        {
            TabTitle = tabTitle;
            foreach (var item in blocks)
            {
                Add(item);
            }
        }
    }
}
