// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        private static readonly HashSet<Type> s_blockWhiteList = new HashSet<Type> { typeof(HeadingBlock) /*header*/, typeof(YamlFrontMatterBlock) /*yaml header*/, typeof(HtmlBlock) /*comment*/ };

        public static (List<Error> errors, TableOfContentsInputModel model) LoadMdTocModel(string tocContent, string filePath)
        {
            var errors = new List<Error>();
            var headingBlocks = new List<HeadingBlock>();

            var (ast, result) = Markup.Parse(tocContent);
            errors.AddRange(result.Errors);
            foreach (var block in ast)
            {
                if (!s_blockWhiteList.Contains(block.GetType()))
                {
                    errors.Add(Errors.InvalidTocSyntax(new Range(block.Line, block.Column), filePath, tocContent.Substring(block.Span.Start, block.Span.Length)));
                }

                if (block is HeadingBlock headingBlock)
                {
                    headingBlocks.Add(headingBlock);
                }
            }

            var tocModel = new TableOfContentsInputModel
            {
                Metadata = result.Metadata,
            };

            try
            {
                tocModel.Items = ConvertTo(filePath, headingBlocks.ToArray(), errors).children;
            }
            catch (DocfxException ex)
            {
                errors.Add(ex.Error);
            }

            return (errors, tocModel);
        }

        private static (List<TableOfContentsInputItem> children, int count) ConvertTo(string filePath, HeadingBlock[] headingBlocks, List<Error> errors, int startIndex = 0)
        {
            if (headingBlocks.Length == 0)
            {
                return (new List<TableOfContentsInputItem>(), 0);
            }

            Debug.Assert(startIndex < headingBlocks.Length);

            int i = startIndex;
            var items = new List<TableOfContentsInputItem>();
            var childrenCount = 0;
            do
            {
                var item = GetItem(headingBlocks[i]);
                items.Add(item);
                var currentLevel = headingBlocks[i].Level;
                if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level > currentLevel)
                {
                    if (headingBlocks[i + 1].Level - currentLevel > 1)
                    {
                        throw Errors.InvalidTocLevel(filePath, currentLevel, headingBlocks[i + 1].Level).ToException();
                    }

                    var (children, count) = ConvertTo(filePath, headingBlocks, errors, i + 1);
                    item.Items = children;
                    i += count;
                    childrenCount += count;
                }

                if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level < currentLevel)
                {
                    break;
                }
            }
            while (++i < headingBlocks.Length);

            return (items, items.Count + childrenCount);

            TableOfContentsInputItem GetItem(HeadingBlock block)
            {
                if (block.Inline == null || !block.Inline.Any())
                {
                    errors.Add(Errors.MissingTocHead(new Range(block.Line, block.Column), filePath));
                    return new TableOfContentsInputItem();
                }

                string name = null;
                string displayName = null;
                string href = null;
                var link = block.Inline.FirstOrDefault(l => l is LinkInline);
                if (link != null && link is LinkInline linkInline)
                {
                    if (!string.IsNullOrEmpty(linkInline.Url))
                        href = linkInline.Url;
                    if (!string.IsNullOrEmpty(linkInline.Title))
                        displayName = linkInline.Title;
                    var literal = linkInline.FirstOrDefault(l => l is LiteralInline);
                    if (literal != null && literal is LiteralInline literalInline)
                    {
                        name = literalInline.Content.ToString();
                    }
                }
                else
                {
                    var literal = block.Inline.FirstOrDefault(l => l is LiteralInline);
                    if (literal != null && literal is LiteralInline literalInline)
                    {
                        name = literalInline.Content.ToString();
                    }
                }

                var currentItem = new TableOfContentsInputItem
                {
                    DisplayName = displayName,
                    Name = name,
                    Href = href,
                };

                return currentItem;
            }
        }
    }
}
