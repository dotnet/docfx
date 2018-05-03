// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class NodeState : TableOfContentsParseState
    {
        public NodeState(TableOfContentsParseState state, int level)
        {
            Level = level;
            Parents = state.Parents;
            Root = state.Root;
            FilePath = state.FilePath;
        }

        public override int Level { get; }

        public override Stack<TableOfContentsInputItem> Parents { get; }

        public override List<TableOfContentsInputItem> Root { get; }

        public override string FilePath { get; }
    }
}
