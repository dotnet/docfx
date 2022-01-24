// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class CodeSnippetInteractiveRewriter : InteractiveBaseRewriter
{
    public override IMarkdownObject Rewrite(IMarkdownObject markdownObject)
    {
        if (markdownObject is CodeSnippet codeSnippet)
        {
            codeSnippet.Language = GetLanguage(codeSnippet.Language, out var isInteractive);
            codeSnippet.IsInteractive = isInteractive;

            if (isInteractive)
            {
                var url = GetGitUrl();
                if (!string.IsNullOrEmpty(url))
                {
                    codeSnippet.GitUrl = url;
                }

                return codeSnippet;
            }
        }

        return markdownObject;
    }

    private static string GetGitUrl()
    {
        // TODO: Disable to get git URL of code snippet
        return null;
    }
}
