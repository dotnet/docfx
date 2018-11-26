// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax;

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

        private string GetGitUrl(CodeSnippet obj)
        {
            // TODO: Disable to get git URL of code snippet
            return null;
        }
    }
}
