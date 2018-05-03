// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal abstract class TableOfContentsParseState
    {
        public abstract int Level { get; }

        public abstract Stack<TableOfContentsItem> Parents { get; }

        public abstract List<TableOfContentsItem> Root { get; }

        public abstract string FilePath { get; }

        public virtual TableOfContentsParseState ApplyRules(TableOfContentsParseRule[] rules, ref string input, ref int lineNumber)
        {
            foreach (var rule in rules)
            {
                var m = rule.Match(input);
                if (m.Success)
                {
                    input = input.Substring(m.Length);
                    lineNumber += m.Value.Count(ch => ch == '\n');
                    return rule.Apply(this, m);
                }
            }
            var message = string.Join(Environment.NewLine, input.Split('\n').Take(3));
            return new ErrorState(this, Level, $"Unknown syntax at line {lineNumber}:{Environment.NewLine}{message}");
        }
    }

    internal sealed class InitialState : TableOfContentsParseState
    {
        public InitialState()
        {
            Parents = new Stack<TableOfContentsItem>();
            Root = new List<TableOfContentsItem>();
        }

        public override int Level => 0;

        public override Stack<TableOfContentsItem> Parents { get; }

        public override string FilePath { get; }

        public override List<TableOfContentsItem> Root { get; }
    }

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

        public override Stack<TableOfContentsItem> Parents { get; }

        public override List<TableOfContentsItem> Root { get; }

        public override string FilePath { get; }
    }

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

        public override Stack<TableOfContentsItem> Parents { get; }

        public override List<TableOfContentsItem> Root { get; }

        public override string FilePath { get; }

        public override TableOfContentsParseState ApplyRules(TableOfContentsParseRule[] rules, ref string input, ref int lineNumber)
        {
            throw new FormatException($"Invalid toc file, Details: {Message}");
        }
    }
}
