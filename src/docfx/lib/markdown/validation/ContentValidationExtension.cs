// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.Docs.Validation;
using Validations.DocFx.Adapter;

#pragma warning disable CS0618

namespace Microsoft.Docs.Build
{
    internal static class ContentValidationExtension
    {
        public static MarkdownPipelineBuilder UseContentValidation(
            this MarkdownPipelineBuilder builder,
            OnlineServiceMarkdownValidatorProvider? validatorProvider,
            Func<List<Heading>, Dictionary<Document, (List<Heading> headings, bool isIncluded)>> getHeadings,
            Func<string, MarkdownObject, (string? content, object? file)> readFile)
        {
            var validators = validatorProvider?.GetValidators();
            return builder.Use(document =>
            {
                if (((Document)InclusionContext.File).FilePath.Format == FileFormat.Markdown)
                {
                    var documentHeadings = new List<Heading>();
                    string[]? monikers = null;
                    string? zoneTarget = null;
                    document.Visit(
                        node =>
                        {
                            if (node is MonikerRangeBlock monikerRangeBlock)
                            {
                                monikers = monikerRangeBlock.GetAttributes().Properties.FirstOrDefault(p => p.Key == "data-moniker").Value?.Split(" ");
                            }

                            if (node is TripleColonBlock tripleColonBlock && tripleColonBlock.Extension.Name == "zone")
                            {
                                zoneTarget = tripleColonBlock.Attributes.TryGetValue("target", out var target) ? target : null;
                            }

                            if (node is HeadingBlock headingBlock)
                            {
                                documentHeadings.Add(new Heading
                                {
                                    // Monikers = monikers,
                                    // ZoneTarget = zoneTarget,
                                    Level = headingBlock.Level,
                                    SourceInfo = headingBlock.ToSourceInfo(),
                                    Content = GetHeadingContent(headingBlock), // used for reporting
                                    HeadingChar = headingBlock.HeaderChar,
                                    RenderedPlainText = MarkdigUtility.ToPlainText(headingBlock), // used for validation
                                });
                            }

                            if (node is InclusionBlock || node is InclusionInline)
                            {
                                var includedFilePath = node is InclusionInline inline ? inline.IncludedFilePath : ((InclusionBlock)node).IncludedFilePath;
                                var inclusionDocument = (Document?)readFile(includedFilePath, node).file;
                                if (inclusionDocument != null)
                                {
                                    documentHeadings.Add(new Heading
                                    {
                                        // Monikers = monikers,
                                        // ZoneTarget = zoneTarget,
                                        Level = -1,
                                        SourceInfo = node.ToSourceInfo(),
                                        Content = inclusionDocument.FilePath.ToString(),
                                    });
                                }
                            }

                            return false;
                        },
                        node =>
                        {
                            // moniker range and triple colon zone can NOT be nested
                            if (node is MonikerRangeBlock)
                            {
                                monikers = null;
                            }

                            if (node is TripleColonBlock tripleColonBlock && tripleColonBlock.Extension.Name == "zone")
                            {
                                zoneTarget = null;
                            }
                        });

                    var allHeadings = getHeadings(documentHeadings);

                    if (InclusionContext.IsInclude)
                    {
                        foreach (var (doc, (headings, _)) in allHeadings)
                        {
                            var currentFile = (Document)InclusionContext.File;
                            if (doc == currentFile)
                            {
                                continue;
                            }

                            var index = 0;
                            while (index < headings.Count)
                            {
                                var current = headings[index];
                                if (current.Level == -1 && current.Content == $"{currentFile.FilePath}")
                                {
                                    headings.RemoveAt(index);
                                    headings.InsertRange(index, documentHeadings.Select(d => new Heading
                                    {
                                        // Monikers = d.Monikers ?? current.Monikers,
                                        // ZoneTarget = d.ZoneTarget ?? current.ZoneTarget,
                                        Content = d.Content,
                                        Level = d.Level,
                                        SourceInfo = current.SourceInfo,
                                        HeadingChar = d.HeadingChar,
                                        RenderedPlainText = d.RenderedPlainText,
                                        InclusionSourceInfo = d.SourceInfo,
                                    }));
                                    index += documentHeadings.Count - 1;
                                }

                                index++;
                            }
                        }
                    }

                    if (validators != null)
                    {
                        foreach (var validator in validators)
                        {
                            validator.Validate(document);
                        }
                    }
                }
            });
        }

        private static string GetHeadingContent(HeadingBlock headingBlock)
        {
            if (headingBlock.Inline is null || !headingBlock.Inline.Any())
            {
                return string.Empty;
            }

            return GetContainerInlineContent(headingBlock.Inline);
            static string GetContainerInlineContent(ContainerInline containerInline)
            {
                var content = new StringBuilder();
                var child = containerInline.FirstChild;
                while (child != null)
                {
                    if (child is LiteralInline childLiteralInline)
                    {
                        content.Append(childLiteralInline.Content.Text, childLiteralInline.Content.Start, childLiteralInline.Content.Length);
                    }

                    if (child is HtmlInline childHtmlInline)
                    {
                        content.Append(childHtmlInline.Tag);
                    }

                    if (child is ContainerInline childContainerInline)
                    {
                        content.Append(GetContainerInlineContent(childContainerInline));
                    }

                    child = child.NextSibling;
                }

                return content.ToString();
            }
        }
    }
}
