// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal class LinkInfo
    {
        public SourceInfo<string> Href { get; init; }

        public LinkAttributeType AttributeType { get; init; }

        public MarkdownObject? MarkdownObject { get; init; }

        public string? AltText { get; init; }

        public string? ImageType { get; init; }

        public int HtmlSourceIndex { get; init; }
    }
}
