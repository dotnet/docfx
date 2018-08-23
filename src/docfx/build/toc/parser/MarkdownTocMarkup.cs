// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownTocMarkup
    {
        private static readonly MarkdownPipeline s_markdownPipeline = CreateMarkdownPipeline();

        private static readonly HashSet<Type> s_blockWhiteList = new HashSet<Type> { typeof(HeadingBlock) /*header*/, typeof(YamlFrontMatterBlock) /*yaml header*/, typeof(HtmlBlock) /*comment*/ };

        [ThreadStatic]
        private static List<Error> t_errors;

        [ThreadStatic]
        private static TableOfContentsInputModel t_model;

        public static (List<Error> errors, TableOfContentsInputModel model) LoadMdTocModel(string tocContent, string filePath)
        {
            t_errors = new List<Error>();
            t_model = new TableOfContentsInputModel();
            var blockContainer = Markdown.Parse(tocContent, s_markdownPipeline);

            var tocModel = new TableOfContentsInputModel
            {
                Metadata = t_model.Metadata,
            };

            var headingBlocks = new List<HeadingBlock>();
            foreach (var block in blockContainer)
            {
                if (!s_blockWhiteList.Contains(block.GetType()))
                {
                    t_errors.Add(Errors.InvalidTocSyntax(new Range(block.Line, block.Column), filePath, tocContent.Substring(block.Span.Start, block.Span.Length)));
                }

                if (block is HeadingBlock headingBlock)
                {
                    headingBlocks.Add(headingBlock);
                }
            }

            try
            {
                tocModel.Items = ConvertTo(filePath, headingBlocks.ToArray()).children;
            }
            catch (DocfxException ex)
            {
                t_errors.Add(ex.Error);
            }

            return (t_errors, tocModel);
        }

        private static (List<TableOfContentsInputItem> children, int count) ConvertTo(string filePath, HeadingBlock[] headingBlocks, int startIndex = 0)
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
                if (item != null)
                {
                    items.Add(item);
                    var currentLevel = headingBlocks[i].Level;
                    if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level > currentLevel)
                    {
                        if (headingBlocks[i + 1].Level - currentLevel > 1)
                        {
                            throw Errors.InvalidTocLevel(filePath).ToException();
                        }

                        var (children, count) = ConvertTo(filePath, headingBlocks, i + 1);
                        item.Items = children;
                        i += count;
                        childrenCount += count;
                    }

                    if (i + 1 < headingBlocks.Length && headingBlocks[i + 1].Level < currentLevel)
                    {
                        break;
                    }
                }
            }
            while (++i < headingBlocks.Length);

            return (items, items.Count + childrenCount);

            TableOfContentsInputItem GetItem(HeadingBlock block)
            {
                if (block.Inline == null || !block.Inline.Any())
                {
                    return null;
                }

                string name = null;
                string displayName = null;
                string href = null;

                if (block.Inline.First() is LinkInline linkInline)
                {
                    if (!string.IsNullOrEmpty(linkInline.Url))
                        href = linkInline.Url;
                    if (!string.IsNullOrEmpty(linkInline.Title))
                        displayName = linkInline.Title;

                    if (linkInline.Any() && linkInline.First() is LiteralInline literalInline)
                    {
                        name = literalInline.Content.ToString();
                    }
                }
                else if (block.Inline.First() is LiteralInline literalInline)
                {
                    name = literalInline.Content.ToString();
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

        private static MarkdownPipeline CreateMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(null, LogWarning, LogError, null, null);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseExtractTocYamlHeader()
                .Build();
        }

        private static void LogError(string code, string message, string doc, int line)
        {
            t_errors.Add(new Error(ErrorLevel.Error, code, message, doc, new Range(line, 0)));
        }

        private static void LogWarning(string code, string message, string doc, int line)
        {
            t_errors.Add(new Error(ErrorLevel.Warning, code, message, doc, new Range(line, 0)));
        }

        private static MarkdownPipelineBuilder UseExtractTocYamlHeader(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (InclusionContext.IsInclude)
                    {
                        return false;
                    }

                    if (node is YamlFrontMatterBlock yamlHeader)
                    {
                        // TODO: fix line info in yamlErrors is not accurate due to offset in markdown
                        var (errors, metadata) = Extract(yamlHeader.Lines.ToString());

                        if (metadata != null)
                        {
                            t_model.Metadata = metadata;
                        }

                        t_errors.AddRange(errors);
                        return true;
                    }
                    return false;
                });
            });

            (List<Error> errors, JObject metadata) Extract(string lines)
            {
                var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(lines);

                if (yamlHeaderObj is JObject obj)
                {
                    return (yamlErrors, obj);
                }

                yamlErrors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
                return (yamlErrors, default);
            }
        }
    }
}
