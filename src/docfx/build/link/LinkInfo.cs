// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.Build;

internal class LinkInfo
{
    public SourceInfo<string> Href { get; init; }

    public string TagName { get; init; } = "a";

    public string AttributeName { get; init; } = "href";

    public bool IsImage => TagName == "img";

    public MarkdownObject? MarkdownObject { get; init; }

    public string? AltText { get; init; }

    public string? ImageType { get; init; }

    public int HtmlSourceIndex { get; init; }
}
