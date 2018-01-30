// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;

    using Markdig.Syntax;

    public class CodeSnippetRewriter : IMarkdownObjectRewriter
    {
        public const string InteractivePostfix = "-interactive";

        public void PostProcess(IMarkdownObject markdownObject)
        {
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
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

        private static string GetLanguage(string language, out bool isInteractive)
        {
            isInteractive = false;
            if (language == null)
            {
                return null;
            }
            if (language.EndsWith(InteractivePostfix))
            {
                isInteractive = true;
                return language.Remove(language.Length - InteractivePostfix.Length);
            }
            return language;
        }

        private string GetGitUrl(CodeSnippet obj)
        {
            // TODO: Disable to get git URL of code snippet
            return null;
        }
    }
}
