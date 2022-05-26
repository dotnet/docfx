// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.MarkdigExtensions;

public class TripleColonInline : Inline, ITripleColon
{
    public IDictionary<string, string> RenderProperties { get; set; }

    public ITripleColonExtensionInfo Extension { get; set; }

    public string Body { get; set; }

    public TripleColonInline()
        : base() { }

    public bool Closed { get; set; }

    public bool EndingTripleColons { get; set; }

    public IDictionary<string, string> Attributes { get; set; }

    public int Count { get; }
}
