// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ExtractTitleExtension
    {
        public static MarkdownPipelineBuilder UseExtractTitle(
            this MarkdownPipelineBuilder builder, MarkdownEngine markdownEngine, Func<ConceptualModel?> getConceptual)
        {
            return builder.Use(document =>
            {
                dynamic? heading = null;
                var conceptual = getConceptual();
                if (conceptual is null)
                {
                    return;
                }

                var foundedTitleCount = 0;
                var candidate = GetNextHeadingCandidate(document, foundedTitleCount++);
                if (candidate != null)
                {
                    if (candidate is HeadingBlock headingBlock && headingBlock.Level <= 3)
                    {
                        heading = headingBlock;
                    }
                    else if (candidate is MonikerRangeBlock)
                    {
                        heading = new List<MonikerRangeBlock>();
                        while (candidate is MonikerRangeBlock monikerRangeBlock)
                        {
                            ((List<MonikerRangeBlock>)heading).AddIfNotNull(ExtractTitleFromMonikerZone(monikerRangeBlock));
                            candidate = GetNextHeadingCandidate(document, foundedTitleCount++);
                        }
                    }

                    if (conceptual.Title.Value is null)
                    {
                        conceptual.Title = GetTitle(heading, markdownEngine);
                    }

                    conceptual.RawTitle = GetRawTitle(heading, markdownEngine);
                }
            });
        }

        private static MarkdownObject? GetNextHeadingCandidate(MarkdownDocument document, int skip)
        {
            var (left, headingCadidate) = GetCandidate(document, skip);
            if (left == 0)
            {
                return headingCadidate;
            }
            return default;

            (int left, MarkdownObject? candidate) GetCandidate(MarkdownDocument document, int skip)
            {
                MarkdownObject? candidate = null;
                for (int i = 0; i < document.Count; i++)
                {
                    var token = document[i];
                    if (!token.IsVisible())
                    {
                        continue;
                    }
                    switch (token)
                    {
                        case HeadingBlock heading when heading.Level <= 3:
                            document.RemoveAt(i);
                            candidate = token;
                            break;
                        case MonikerRangeBlock _:
                            candidate = token;
                            break;
                        case InclusionBlock inclusionBlock:
                            (skip, candidate) = GetCandidate((MarkdownDocument)inclusionBlock[0], skip);
                            break;
                        default:
                            skip = -1;
                            break;
                    }
                    if (skip == 0 || skip == -1)
                    {
                        break;
                    }
                    skip--;
                }
                return (skip, candidate);
            }
        }

        private static MonikerRangeBlock? ExtractTitleFromMonikerZone(MonikerRangeBlock monikerRangeBlock)
        {
            for (var i = 0; i < monikerRangeBlock.Count; i++)
            {
                var token = monikerRangeBlock[i];
                if (token is HeadingBlock heading && heading.Level <= 3)
                {
                    var headerBlock = new MonikerRangeBlock(null);
                    var monikers = monikerRangeBlock.GetAttributes().Properties.First(p => p.Key == "data-moniker").Value;
                    headerBlock.GetAttributes().AddPropertyIfNotExist("data-moniker", monikers);
                    monikerRangeBlock.RemoveAt(i);
                    headerBlock.Insert(0, token);
                    return headerBlock;
                }
                else if (token.IsVisible())
                {
                    break;
                }
            }
            return null;
        }

        private static SourceInfo<string?> GetTitle(dynamic? heading, MarkdownEngine markdownEngine)
        {
            HeadingBlock? headingBlock = heading is List<MonikerRangeBlock> monikerRangeList
                                    ? monikerRangeList.FirstOrDefault()?[0]
                                    : heading;

            if (headingBlock != null && headingBlock.Inline.Any())
            {
                return new SourceInfo<string?>(markdownEngine.ToPlainText(headingBlock), headingBlock.GetSourceInfo());
            }
            else
            {
                return new SourceInfo<string?>(null);
            }
        }

        private static string? GetRawTitle(dynamic? heading, MarkdownEngine markdownEngine)
        {
            var rawTitle = string.Empty;

            if (heading is HeadingBlock headingBlock)
            {
                rawTitle = markdownEngine.ToHtml(heading);
            }
            else if (heading is List<MonikerRangeBlock> monikerRangeList)
            {
                monikerRangeList.ForEach(monikerRangeBlock => rawTitle += markdownEngine.ToHtml(monikerRangeBlock.FindBlockAtPosition(0)));
            }
            return rawTitle;
        }
    }
}
