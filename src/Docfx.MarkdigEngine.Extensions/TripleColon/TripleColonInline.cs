// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Parsers;
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class TripleColonInline : ContainerInline, ITripleColon
{
    public IDictionary<string, string> RenderProperties { get; set; }
    public ITripleColonExtensionInfo Extension { get; set; }
    public TripleColonInline(InlineParser parser) : base() { }
    public bool Closed { get; set; }
    public bool EndingTripleColons { get; set; }
    public IDictionary<string, string> Attributes { get; set; }
    public int Count { get; }
}
