// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// Markdown engine
    /// </summary>
    public interface IMarkdownEngine
    {
        /// <summary>
        /// Parser (it can read markdown text, then return markdown tokens).
        /// </summary>
        IMarkdownParser Parser { get; }
        /// <summary>
        /// Renderer (it can read markdown token, then return text e.g. html).
        /// </summary>
        IMarkdownRenderer Renderer { get; }
        /// <summary>
        /// Writer (it can read markdown tokens, then rewrite them and return).
        /// </summary>
        IMarkdownRewriteEngine RewriteEngine { get; }

        /// <summary>
        /// Mark markdown text.
        /// </summary>
        /// <param name="markdown">The markdown text.</param>
        /// <param name="context">The markdown context contains rules.</param>
        /// <returns>Rendered text.</returns>
        StringBuffer Mark(string markdown, IMarkdownContext context = null);
        /// <summary>
        /// Mark markdown text.
        /// </summary>
        /// <param name="markdown">The markdown text.</param>
        /// <returns>Rendered text.</returns>
        string Markup(string markdown);
    }
}