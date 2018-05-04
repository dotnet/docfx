// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorState : TableOfContentsParseState
    {
        public ErrorState(TableOfContentsParseState state, int level, string message)
        {
            Level = level;
            Parents = state.Parents;
            Root = state.Root;
            FilePath = state.FilePath;
            Message = message;
        }

        public string Message { get; }

        public override int Level { get; }

        public override Stack<TableOfContentsInputItem> Parents { get; }

        public override List<TableOfContentsInputItem> Root { get; }

        public override string FilePath { get; }

        public override TableOfContentsParseState ApplyRules(TableOfContentsParseRule[] rules, ref string input, ref int lineNumber)
        {
            throw new FormatException($"Invalid toc file, Details: {Message}");
        }
    }
}
