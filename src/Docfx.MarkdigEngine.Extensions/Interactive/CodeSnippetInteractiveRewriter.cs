// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class CodeSnippetInteractiveRewriter : InteractiveBaseRewriter
{
    public override IMarkdownObject Rewrite(IMarkdownObject markdownObject)
    {
        if (markdownObject is CodeSnippet codeSnippet)
        {
            codeSnippet.Language = GetLanguage(codeSnippet.Language, out bool isInteractive);
            codeSnippet.IsInteractive = isInteractive;

            if (isInteractive)
            {
                var url = GetGitUrl(codeSnippet);
                if (!string.IsNullOrEmpty(url))
                {
                    codeSnippet.GitUrl = url;
                }

                return codeSnippet;
            }
        }

        return markdownObject;
    }

    private static string GetGitUrl(CodeSnippet obj)
    {
        // TODO: Disable to get git URL of code snippet
        return null;
    }
}
