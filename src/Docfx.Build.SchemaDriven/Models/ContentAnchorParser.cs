// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven;

public class ContentAnchorParser : IContentAnchorParser
{
    public const string AnchorContentName = "*content";

    public string Content { get; }

    public bool ContainsAnchor { get; private set; }

    public ContentAnchorParser(string content)
    {
        Content = content;
    }

    public string Parse(string input)
    {
        if (input != null && input.Trim() == AnchorContentName)
        {
            ContainsAnchor = true;
            return Content;
        }

        return input;
    }
}
