// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlAgilityPack;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal interface IMarkdownEngine
    {
        (Error[] errors, MarkdownDocument ast) Parse(string markdown, MarkdownPipelineType piplineType);

        (Error[] errors, HtmlNode html) ToHtml(string markdown, FilePath file, MarkdownPipelineType pipelineType);
    }
}
