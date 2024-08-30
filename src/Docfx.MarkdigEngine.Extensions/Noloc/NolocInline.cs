// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class NolocInline : LeafInline
{
    public string Text { get; set; }
}
