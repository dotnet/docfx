// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal abstract class TableOfContentsParseRule
    {
        public abstract Match Match(string text);

        public abstract TableOfContentsParseState Apply(TableOfContentsParseState state, Match match);

        protected TableOfContentsParseState ApplyCore(TableOfContentsParseState state, int level, string text, string href, string displayText = null)
        {
            if (level > state.Level + 1)
            {
                return new ErrorState(state, level, $"Skip level is not allowed. Toc content: {text}");
            }

            // If current node is another node in higher or same level
            for (int i = state.Level; i >= level; --i)
            {
                state.Parents.Pop();
            }

            var item = new TableOfContentsInputItem
            {
                Name = text,
                DisplayName = displayText,
                Href = href,
            };
            if (state.Parents.Count > 0)
            {
                var parent = state.Parents.Peek();
                if (parent.Items == null)
                {
                    parent.Items = new List<TableOfContentsInputItem>();
                }
                parent.Items.Add(item);
            }
            else
            {
                state.Root.Add(item);
            }
            state.Parents.Push(item);

            if (state.Level == level)
            {
                return state;
            }
            return new NodeState(state, level);
        }
    }

}
