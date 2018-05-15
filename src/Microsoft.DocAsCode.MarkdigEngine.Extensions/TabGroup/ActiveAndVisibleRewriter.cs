// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Markdig.Syntax;

    public class ActiveAndVisibleRewriter : IMarkdownObjectRewriter
    {
        private readonly MarkdownContext _context;
        private List<string[]> tabSelectionInfo = new List<string[]>();

        public ActiveAndVisibleRewriter(MarkdownContext context)
        {
            _context = context;
        }

        public void PostProcess(IMarkdownObject markdownObject)
        {
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            if (markdownObject is TabGroupBlock block)
            {
                var items = block.Items.ToList();
                var firstVisibleTab = ApplyTabVisible(tabSelectionInfo, items);
                var idAndCountList = GetTabIdAndCountList(items).ToList();
                if (idAndCountList.Any(g => g.Item2 > 1))
                {
                    _context.LogWarning(
                        "invalid-tab-group",
                        $"Duplicate tab id: {string.Join(",", idAndCountList.Where(g => g.Item2 > 1))}.");
                }
                var active = GetTabActive(block, tabSelectionInfo, items, firstVisibleTab, idAndCountList);
                block.ActiveTabIndex = active;
                block.Items = items.ToImmutableArray();

                return block;
            }

            return markdownObject;
        }


        private int ApplyTabVisible(List<string[]> tabSelectionInfo, List<TabItemBlock> items)
        {
            var firstVisibleTab = -1;

            for (var i = 0; i < items.Count; i++)
            {
                var tab = items[i];
                var visible = string.IsNullOrEmpty(tab.Condition) || tabSelectionInfo.Any(t => t[0] == tab.Condition);
                if (visible && firstVisibleTab == -1)
                {
                    firstVisibleTab = i;
                }
                if (tab.Visible != visible)
                {
                    items[i] = new TabItemBlock(tab.Id, tab.Condition, tab.Title, tab.Content, visible);
                }
            }

            return firstVisibleTab;
        }

        private IEnumerable<Tuple<string, int>> GetTabIdAndCountList(List<TabItemBlock> items) =>
            from tab in items
            where tab.Visible
            from id in tab.Id.Split('+')
            group id by id into g
            select Tuple.Create(g.Key, g.Count());

        private int GetTabActive(TabGroupBlock block, List<string[]> tabSelectionInfo, List<TabItemBlock> items, int firstVisibleTab, List<Tuple<string, int>> idAndCountList)
        {
            var active = -1;
            var hasDifferentSet = false;
            foreach (var info in tabSelectionInfo)
            {
                var set = info.Intersect(from pair in idAndCountList select pair.Item1).ToList();
                if (set.Count > 0)
                {
                    if (set.Count == info.Length && set.Count == idAndCountList.Count)
                    {
                        active = FindActiveIndex(items, info);
                        break;
                    }
                    else
                    {
                        hasDifferentSet = true;
                        active = FindActiveIndex(items, info);
                        if (active != -1)
                        {
                            break;
                        }
                    }
                }
            }

            if (hasDifferentSet)
            {
                _context.LogWarning("invalid-tab-group", "Tab group with different tab id set.");
            }

            if (active == -1)
            {
                if (firstVisibleTab != -1)
                {
                    active = firstVisibleTab;
                    tabSelectionInfo.Add((from pair in idAndCountList select pair.Item1).ToArray());
                }
                else
                {
                    active = 0;
                    _context.LogWarning("invalid-tab-group", "All tabs are hidden in the tab group.");
                }
            }

            return active;
        }


        private int FindActiveIndex(List<TabItemBlock> items, string[] info)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.Visible)
                {
                    continue;
                }
                if (Array.IndexOf(item.Id.Split('+'), info[0]) != -1)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
