// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;
    using System.Collections.Generic;

    public class TripleColonBlock : ContainerBlock
    {
        public IDictionary<string, string> RenderProperties { get; set; }
        public ITripleColonExtensionInfo Extension { get; set; }
        public TripleColonBlock(BlockParser parser) : base(parser) { }
        public bool Closed { get; set; }
    }
}
