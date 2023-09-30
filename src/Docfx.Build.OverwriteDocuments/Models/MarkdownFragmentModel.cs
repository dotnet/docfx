// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.Build.OverwriteDocuments;

public class MarkdownFragmentModel
{
    public string Uid { get; set; }

    public Block UidSource { get; set; }

    public string YamlCodeBlock { get; set; }

    public Block YamlCodeBlockSource { get; set; }

    public List<MarkdownPropertyModel> Contents { get; set; }
}
