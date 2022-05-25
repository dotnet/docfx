// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using System.Collections.Generic;

    public class TripleColonInline : ContainerInline, ITripleColon
    {
        public IDictionary<string, string> RenderProperties { get; set; }
        public ITripleColonExtensionInfo Extension { get; set; }
        public string Body { get; set; }
        public TripleColonInline(InlineParser parser) : base() { }
        public bool Closed { get; set; }
        public bool EndingTripleColons { get; set; }
        public IDictionary<string, string> Attributes { get; set; }
        public int Count { get; }
    }
}
