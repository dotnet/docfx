// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    public class DfmEngineBuilder : GfmEngineBuilder
    {
        private readonly string _baseDir;
        private IReadOnlyList<string> _fallbackFolders;

        public DfmEngineBuilder(Options options, string baseDir = null, string templateDir = null, IReadOnlyList<string> fallbackFolders = null)
            : this(options, baseDir, templateDir, fallbackFolders, null)
        {
        }

        public DfmEngineBuilder(Options options, string baseDir, string templateDir, IReadOnlyList<string> fallbackFolders, ICompositionContainer container) : base(options)
        {
            _baseDir = baseDir ?? string.Empty;
            _fallbackFolders = fallbackFolders ?? new List<string>();
            var inlineRules = InlineRules.ToList();

            // xref auto link must be before MarkdownAutoLinkInlineRule
            var index = inlineRules.FindIndex(s => s is MarkdownAutoLinkInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownAutoLinkInlineRule should exist!");
            }
            inlineRules.Insert(index, new DfmXrefAutoLinkInlineRule());

            index = inlineRules.FindLastIndex(s => s is MarkdownLinkInlineRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownLinkInlineRule should exist!");
            }
            inlineRules.Insert(index + 1, new DfmXrefShortcutInlineRule());
            inlineRules.Insert(index + 1, new DfmEmailInlineRule());
            inlineRules.Insert(index + 1, new DfmFencesInlineRule());

            // xref link inline rule must be before MarkdownLinkInlineRule
            inlineRules.Insert(index, new DfmIncludeInlineRule());

            Replace<MarkdownTextInlineRule, DfmTextInlineRule>(inlineRules);

            var blockRules = BlockRules.ToList();
            index = blockRules.FindLastIndex(s => s is MarkdownCodeBlockRule);
            if (index < 0)
            {
                throw new ArgumentException("MarkdownNewLineBlockRule should exist!");
            }

            blockRules.InsertRange(
                index + 1,
                new IMarkdownRule[]
                {
                    new DfmIncludeBlockRule(),
                    new DfmVideoBlockRule(),
                    new DfmYamlHeaderBlockRule(),
                    new DfmSectionBlockRule(),
                    new DfmFencesBlockRule(),
                    new DfmNoteBlockRule()
                });

            Replace<MarkdownBlockquoteBlockRule, DfmBlockquoteBlockRule>(blockRules);
            Replace<MarkdownTableBlockRule, DfmTableBlockRule>(blockRules);
            Replace<MarkdownNpTableBlockRule, DfmNpTableBlockRule>(blockRules);

            InlineRules = inlineRules.ToImmutableList();
            BlockRules = blockRules.ToImmutableList();

            Rewriter = InitMarkdownStyle(container, baseDir, templateDir);
            TokenAggregators = ImmutableList.Create<IMarkdownTokenAggregator>(
                new HeadingIdAggregator(),
                new TabGroupAggregator());
        }

        private static void Replace<TSource, TReplacement>(List<IMarkdownRule> blockRules)
            where TSource : IMarkdownRule
            where TReplacement : IMarkdownRule, new()
        {
            var index = blockRules.FindIndex(item => item is TSource);
            if (index < 0)
            {
                throw new ArgumentException($"{typeof(TSource).Name} should exist!");
            }
            blockRules[index] = new TReplacement();
        }

        private static Func<IMarkdownRewriteEngine, DfmTabGroupBlockToken, IMarkdownToken> GetTabGroupIdRewriter()
        {
            var dict = new Dictionary<string, int>();
            var tabSelectionInfo = new List<string[]>();
            var selectedTabIds = new HashSet<string>();
            return (IMarkdownRewriteEngine engine, DfmTabGroupBlockToken token) =>
            {
                var newToken = RewriteActiveAndVisible(
                    RewriteGroupId(token, dict),
                    tabSelectionInfo);
                if (token == newToken)
                {
                    return null;
                }
                return newToken;
            };
        }

        private static DfmTabGroupBlockToken RewriteGroupId(DfmTabGroupBlockToken token, Dictionary<string, int> dict)
        {
            var groupId = token.Id;
            while (true)
            {
                if (!dict.TryGetValue(groupId, out int index))
                {
                    dict.Add(groupId, 1);
                    break;
                }
                else
                {
                    dict[groupId]++;
                    groupId = groupId + "-" + index.ToString();
                }
            }
            if (token.Id == groupId)
            {
                return token;
            }
            return new DfmTabGroupBlockToken(token.Rule, token.Context, groupId, token.Items, token.ActiveTabIndex, token.SourceInfo);
        }

        private static DfmTabGroupBlockToken RewriteActiveAndVisible(DfmTabGroupBlockToken token, List<string[]> tabSelectionInfo)
        {
            var items = token.Items.ToList();
            int firstVisibleTab = ApplyTabVisible(tabSelectionInfo, items);
            var idAndCountList = GetTabIdAndCountList(items).ToList();
            if (idAndCountList.Any(g => g.Item2 > 1))
            {
                Logger.LogWarning($"Duplicate tab id: {string.Join(",", idAndCountList.Where(g => g.Item2 > 1))}.", line: token.SourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.DuplicateTabId);
            }
            var active = GetTabActive(token, tabSelectionInfo, items, firstVisibleTab, idAndCountList);
            return new DfmTabGroupBlockToken(token.Rule, token.Context, token.Id, items.ToImmutableArray(), active, token.SourceInfo);
        }

        private static int ApplyTabVisible(List<string[]> tabSelectionInfo, List<DfmTabItemBlockToken> items)
        {
            int firstVisibleTab = -1;

            for (int i = 0; i < items.Count; i++)
            {
                var tab = items[i];
                var visible = string.IsNullOrEmpty(tab.Condition) || tabSelectionInfo.Any(t => t[0] == tab.Condition);
                if (visible && firstVisibleTab == -1)
                {
                    firstVisibleTab = i;
                }
                if (tab.Visible != visible)
                {
                    items[i] = new DfmTabItemBlockToken(tab.Rule, tab.Context, tab.Id, tab.Condition, tab.Title, tab.Content, visible, tab.SourceInfo);
                }
            }

            return firstVisibleTab;
        }

        private static IEnumerable<Tuple<string, int>> GetTabIdAndCountList(List<DfmTabItemBlockToken> items) =>
            from tab in items
            where tab.Visible
            from id in tab.Id.Split('+')
            group id by id into g
            select Tuple.Create(g.Key, g.Count());

        private static int GetTabActive(DfmTabGroupBlockToken token, List<string[]> tabSelectionInfo, List<DfmTabItemBlockToken> items, int firstVisibleTab, List<Tuple<string, int>> idAndCountList)
        {
            int active = -1;
            bool hasDifferentSet = false;
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
                Logger.LogWarning("Tab group with different tab id set.", line: token.SourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.DifferentTabIdSet);
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
                    Logger.LogWarning("All tabs are hidden in the tab group.", file: token.SourceInfo.File, line: token.SourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.NoVisibleTab);
                }
            }

            return active;
        }

        private static int FindActiveIndex(List<DfmTabItemBlockToken> items, string[] info)
        {
            for (int i = 0; i < items.Count; i++)
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

        private static IMarkdownTokenRewriter InitMarkdownStyle(ICompositionContainer container, string baseDir, string templateDir)
        {
            try
            {
                return MarkdownValidatorBuilder.Create(container, baseDir, templateDir).CreateRewriter();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.ToString()}");
            }
            return null;
        }

        public DfmEngine CreateDfmEngine(object renderer)
        {
            return new DfmEngine(
                CreateParseContext().SetBaseFolder(_baseDir ?? string.Empty).SetFallbackFolders(_fallbackFolders),
                MarkdownTokenRewriterFactory.Composite(
                    MarkdownTokenRewriterFactory.FromLambda(GetTabGroupIdRewriter()),
                    Rewriter),
                renderer,
                Options)
            {
                TokenTreeValidator = TokenTreeValidator,
                TokenAggregators = TokenAggregators,
            };
        }

        public override IMarkdownEngine CreateEngine(object renderer)
        {
            return CreateDfmEngine(renderer);
        }
    }
}
