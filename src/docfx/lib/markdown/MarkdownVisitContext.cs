// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class MarkdownVisitContext
    {
        public Stack<SourceInfo<Document>> FileStack { get; private set; }

        public IEnumerable<SourceInfo?> Parents => FileStack.Reverse().Skip(1).Select(f => f.Source);

        public Document Document => FileStack.Peek();

        public bool IsInclude => FileStack.Count >= 2;

        public MarkdownVisitContext(Document document)
        {
            FileStack = new Stack<SourceInfo<Document>>();
            FileStack.Push(new SourceInfo<Document>(document));
        }
    }
}
