// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class InclusionBlock : LeafBlock
    {
        public InclusionContext Context { get; set; }

        public InclusionBlock(BlockParser parser): base(parser)
        {

        }
    }
}
