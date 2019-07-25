// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class ImageBlock : ContainerBlock
    {
        public ImageBlock(BlockParser parser) : base(parser)
        {
        }

        public int ColonCount { get; set; }
        public string Src { get; set; }
        public string Alt { get; set; }
        public string Id { get; set; }
    }
}
