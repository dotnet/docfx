// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class InclusionInline : ContainerInline
{
    public string Title { get; set; }

    public string IncludedFilePath { get; set; }

    public object ResolvedFilePath { get; set; }

    public string GetRawToken() => $"[!include[{Title}]({IncludedFilePath})]";
}
