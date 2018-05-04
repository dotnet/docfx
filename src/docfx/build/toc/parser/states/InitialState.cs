// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class InitialState : TableOfContentsParseState
    {
        public InitialState(string filePath)
        {
            Parents = new Stack<TableOfContentsInputItem>();
            Root = new List<TableOfContentsInputItem>();
            FilePath = filePath;
        }

        public override int Level => 0;

        public override Stack<TableOfContentsInputItem> Parents { get; }

        public override string FilePath { get; }

        public override List<TableOfContentsInputItem> Root { get; }
    }
}
