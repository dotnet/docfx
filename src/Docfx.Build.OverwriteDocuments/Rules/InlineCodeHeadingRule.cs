// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.Build.OverwriteDocuments;

public class InlineCodeHeadingRule : IOverwriteBlockRule
{
    public virtual string TokenName => "InlineCodeHeading";

    protected virtual bool NeedCheckLevel { get; set; }

    protected virtual int Level { get; set; }

    public bool Parse(Block block, out string value)
    {
        ArgumentNullException.ThrowIfNull(block);

        var inline = ParseCore(block);
        value = inline?.Content;
        return inline != null;
    }

    private CodeInline ParseCore(Block block)
    {
        if (block is not HeadingBlock heading
            || NeedCheckLevel && heading.Level != Level
            || heading.Inline.FirstChild != heading.Inline.LastChild)
        {
            return null;
        }

        return heading.Inline.FirstChild as CodeInline;
    }
}
