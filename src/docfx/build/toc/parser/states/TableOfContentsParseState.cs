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

        public abstract Stack<TableOfContentsInputItem> Parents { get; }

        public abstract List<TableOfContentsInputItem> Root { get; }

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
}
