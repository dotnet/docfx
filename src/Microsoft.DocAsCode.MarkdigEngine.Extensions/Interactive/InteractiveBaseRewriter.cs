// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax;

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
            if (language.EndsWith(InteractivePostfix))
            {
                isInteractive = true;
                return language.Remove(language.Length - InteractivePostfix.Length);
            }
            return language;
        }
    }
}
