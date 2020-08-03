// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MarkdownVisitContext
    {
        public Stack<MonikerList> ZoneMonikerStack { get; private set; }

        public Stack<string> ZoneStack { get; private set; }

        public Stack<SourceInfo<Document>> FileStack { get; private set; }

        public IEnumerable<SourceInfo?> Parents => FileStack.Reverse().Skip(1).Select(f => f.Source);

        public int TripleColonCount { get; set; } = 0;

        public Document Document => FileStack.Peek();

        public bool IsInclude => FileStack.Count >= 2;

        public MonikerList ZoneMoniker => ZoneMonikerStack.TryPeek(out var zoneMoniker) ? zoneMoniker : default;

        public MarkdownVisitContext(Document document)
        {
            FileStack = new Stack<SourceInfo<Document>>();
            ZoneMonikerStack = new Stack<MonikerList>();
            ZoneStack = new Stack<string>();
            FileStack.Push(new SourceInfo<Document>(document));
        }
    }
}
