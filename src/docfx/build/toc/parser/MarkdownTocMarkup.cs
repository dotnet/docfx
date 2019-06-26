// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        public static (List<Error> errors, TableOfContentsModel model) LoadMdTocModel(string tocContent, Document file)
        {
            var errors = new List<Error>();
            var headingBlocks = new List<HeadingBlock>();
            var (markupErrors, ast) = MarkdownUtility.Parse(tocContent, MarkdownPipelineType.TocMarkdown);
            errors.AddRange(markupErrors);

            foreach (var block in ast)
            {
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        headingBlocks.Add(headingBlock);
                        break;
                    case YamlFrontMatterBlock _:
                    case HtmlBlock htmlBlock when htmlBlock.Type == HtmlBlockType.Comment:
                        break;
                    default:
                        errors.Add(Errors.InvalidTocSyntax(new SourceInfo<string>(tocContent.Substring(block.Span.Start, block.Span.Length), block.ToSourceInfo(file: file.ToString()))));
                        break;
                }
            }

            var (metaErrors, metadata) = ExtractYamlHeader.Extract(new StringReader(tocContent), file.FilePath);
            errors.AddRange(metaErrors);

            var (validationErrors, tocMetadata) = JsonUtility.ToObject<TableOfContentsMetadata>(metadata);
            errors.AddRange(validationErrors);

            var tocModel = new TableOfContentsModel { Metadata = tocMetadata };
            tocModel.Items = ConvertTo(tocContent, file.FilePath, headingBlocks.ToArray(), errors).children;

            return (errors, tocModel);
        }

        private static (List<TableOfContentsItem> children, int count) ConvertTo(string tocContent, string filePath, HeadingBlock[] headingBlocks, List<Error> errors, int startIndex = 0)
        {
            if (headingBlocks.Length == 0)
            {
                return (new List<TableOfContentsItem>(), 0);
            }

            Debug.Assert(startIndex < headingBlocks.Length);

            int i = startIndex;
            var items = new List<TableOfContentsItem>();
            var childrenCount = 0;
            do
            {
                var item = GetItem(headingBlocks[i]);
                if (item == null)
                {
                    continue;
                }

                items.Add(item);
                var currentLevel = headingBlocks[i].Level;
                var (nextLevelDistance, skipped) = GetNextLevelDistance(i, headingBlocks[i].Level);
                if (nextLevelDistance > 0)
                {
                    var (children, count) = ConvertTo(tocContent, filePath, headingBlocks, errors, i + nextLevelDistance);
                    item.Items = children;
                    i = i + count + skipped;
                    childrenCount += count;
                }

                if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level < currentLevel)
                {
                    break;
                }
            }
            while (++i < headingBlocks.Length);

            return (items, items.Count + childrenCount);

            (int distance, int skipped) GetNextLevelDistance(int currentIndex, int currentLevel)
            {
                int distance = 0;
                int reported = 0;
                for (int j = currentIndex + 1; j < headingBlocks.Length; j++)
                {
                    if (headingBlocks[j].Level <= currentLevel)
                    {
                        break;
                    }

                    distance++;

                    if (headingBlocks[j].Level - currentLevel == 1)
                    {
                        break;
                    }

                    if (reported++ == 0)
                        errors.Add(Errors.InvalidTocLevel(headingBlocks[j].ToSourceInfo(file: filePath), currentLevel, headingBlocks[j].Level));
                }

                return (distance, distance > 0 ? distance - 1 : 0);
            }

            TableOfContentsItem GetItem(HeadingBlock block)
            {
                var currentItem = new TableOfContentsItem();
                if (block.Inline is null || !block.Inline.Any())
                {
                    currentItem.Name = new SourceInfo<string>(null, block.ToSourceInfo(file: filePath));
                    return currentItem;
                }

                if (block.Inline.Count() > 1 && block.Inline.Any(l => l is XrefInline || l is LinkInline))
                {
                    var invalidTocSyntaxContent = tocContent.Substring(block.Span.Start, block.Span.Length);
                    errors.Add(Errors.InvalidTocSyntax(new SourceInfo<string>(invalidTocSyntaxContent, block.ToSourceInfo(file: filePath)), "multiple inlines in one heading block is not allowed"));
                    return null;
                }

                var xrefLink = block.Inline.FirstOrDefault(l => l is XrefInline);
                if (xrefLink != null && xrefLink is XrefInline xrefInline && !string.IsNullOrEmpty(xrefInline.Href))
                {
                    currentItem.Uid = new SourceInfo<string>(xrefInline.Href, xrefInline.ToSourceInfo(file: filePath));
                    return currentItem;
                }

                var link = block.Inline.FirstOrDefault(l => l is LinkInline);
                if (link != null && link is LinkInline linkInline)
                {
                    if (!string.IsNullOrEmpty(linkInline.Url))
                    {
                        currentItem.Href = new SourceInfo<string>(linkInline.Url, linkInline.ToSourceInfo(file: filePath));
                    }
                    if (!string.IsNullOrEmpty(linkInline.Title))
                        currentItem.DisplayName = linkInline.Title;

                    currentItem.Name = GetLiteral(linkInline);
                }

                if (currentItem.Name.Value is null)
                {
                    currentItem.Name = GetLiteral(block.Inline);
                }

                return currentItem;
            }

            SourceInfo<string> GetLiteral(ContainerInline inline)
            {
                var result = new StringBuilder();
                var child = inline.FirstChild;

                while (child != null)
                {
                    if (!(child is LiteralInline literal))
                    {
                        errors.Add(Errors.InvalidTocSyntax(new SourceInfo<string>(tocContent.Substring(inline.Span.Start, inline.Span.Length), inline.ToSourceInfo(file: filePath))));
                        return default;
                    }

                    var content = literal.Content;
                    result.Append(content.Text, content.Start, content.Length);
                    child = child.NextSibling;
                }

                return new SourceInfo<string>(result.ToString(), inline.ToSourceInfo(file: filePath));
            }
        }
    }
}
