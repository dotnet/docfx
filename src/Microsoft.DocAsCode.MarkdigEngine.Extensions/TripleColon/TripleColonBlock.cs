// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class TripleColonBlock : ContainerBlock, ITripleColon
{
    public IDictionary<string, string> RenderProperties { get; set; }
    public ITripleColonExtensionInfo Extension { get; set; }
    public TripleColonBlock(BlockParser parser) : base(parser) { }
    public bool Closed { get; set; }
    public bool EndingTripleColons { get; set; }
    public IDictionary<string, string> Attributes { get; set; }
}

interface ITripleColon
{
    public IDictionary<string, string> RenderProperties { get; set; }
    public ITripleColonExtensionInfo Extension { get; set; }
    public bool Closed { get; set; }
    public bool EndingTripleColons { get; set; }
    public IDictionary<string, string> Attributes { get; set; }
    public int Count { get; }
}
