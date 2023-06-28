// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public interface IMarkdownObjectRewriter
{
    void PreProcess(IMarkdownObject markdownObject);

    IMarkdownObject Rewrite(IMarkdownObject markdownObject);

    void PostProcess(IMarkdownObject markdownObject);
}