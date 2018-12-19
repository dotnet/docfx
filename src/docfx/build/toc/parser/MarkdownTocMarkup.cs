// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        private static readonly HashSet<Type> s_blockWhiteList = new HashSet<Type> { typeof(HeadingBlock) /*header*/, typeof(YamlFrontMatterBlock) /*yaml header*/, typeof(HtmlBlock) /*comment*/ };

        public static (List<Error> errors, TableOfContentsInputModel model) LoadMdTocModel(string tocContent, Document file, Context context)
        {
            var errors = new List<Error>();
            var headingBlocks = new List<HeadingBlock>();
            var (ast, result) = Markup.Parse(tocContent);
            errors.AddRange(result.Errors);
            foreach (var block in ast)
            {
                if (!s_blockWhiteList.Contains(block.GetType()))
                {
                    errors.Add(Errors.InvalidTocSyntax(new Range(block.Line, block.Column), file.FilePath, tocContent.Substring(block.Span.Start, block.Span.Length)));
                }

                if (block is HeadingBlock headingBlock)
                {
                    headingBlocks.Add(headingBlock);
                }
            }

            var (metaErrors, metadata) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(metaErrors);
            var tocModel = new TableOfContentsInputModel
            {
                Metadata = metadata,
            };

            try
            {
                tocModel.Items = ConvertTo(file, headingBlocks.ToArray(), errors).children;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.Add(dex.Error);
            }

            return (errors, tocModel);
        }

        private static (List<TableOfContentsInputItem> children, int count) ConvertTo(Document file, HeadingBlock[] headingBlocks, List<Error> errors, int startIndex = 0)
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
                        throw Errors.InvalidTocLevel(file.FilePath, currentLevel, headingBlocks[i + 1].Level).ToException();
                    }

                    var (children, count) = ConvertTo(file, headingBlocks, errors, i + 1);
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
                var currentItem = new TableOfContentsInputItem();
                if (block.Inline == null || !block.Inline.Any())
                {
                    errors.Add(Errors.MissingTocHead(new Range(block.Line, block.Column), file.FilePath));
                    return currentItem;
                }

                var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
                if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
                {
                    currentItem.Uid = xrefInline.Href;
                    return currentItem;
                }

                var link = block.Inline.FirstOrDefault(l => l is LinkInline);
                var literalLink = block.Inline.FirstOrDefault(l => l is LiteralInline);
                if (link != null && link is LinkInline linkInline)
                {
                    if (!string.IsNullOrEmpty(linkInline.Url))
                        currentItem.Href = linkInline.Url;
                    if (!string.IsNullOrEmpty(linkInline.Title))
                        currentItem.DisplayName = linkInline.Title;

                    literalLink = linkInline.FirstOrDefault(l => l is LiteralInline);
                }

                if (literalLink != null && literalLink is LiteralInline literalInline)
                {
                    currentItem.Name = literalInline.Content.ToString();
                }

                return currentItem;
            }
        }
    }
}
