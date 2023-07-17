// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public abstract class InteractiveBaseRewriter : IMarkdownObjectRewriter
{
    protected const string InteractivePostfix = "-interactive";

    public void PostProcess(IMarkdownObject markdownObject)
    {
    }

    public void PreProcess(IMarkdownObject markdownObject)
    {
    }

    public abstract IMarkdownObject Rewrite(IMarkdownObject markdownObject);

    protected static string GetLanguage(string language, out bool isInteractive)
    {
        isInteractive = false;
        if (language == null)
        {
            return null;
        }
        if (language.EndsWith(InteractivePostfix, StringComparison.Ordinal))
        {
            isInteractive = true;
            return language.Remove(language.Length - InteractivePostfix.Length);
        }
        return language;
    }
}
