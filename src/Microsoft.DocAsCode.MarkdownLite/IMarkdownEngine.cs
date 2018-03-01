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
        /// Get the parser (it can read markdown text, then return markdown tokens).
        /// </summary>
        IMarkdownParser Parser { get; }
        /// <summary>
        /// Get the renderer (it can read markdown token, then return text e.g. html).
        /// </summary>
        IMarkdownRenderer Renderer { get; }
        /// <summary>
        /// Get the rewriter (it can read markdown tokens, then rewrite them and return).
        /// </summary>
        IMarkdownRewriteEngine RewriteEngine { get; }
        /// <summary>
        /// Get the token tree validator.
        /// </summary>
        IMarkdownTokenTreeValidator TokenTreeValidator { get; set; }

        /// <summary>
        /// Mark markdown text.
        /// </summary>
        /// <param name="context">The markdown context contains rules.</param>
        /// <param name="sourceInfo">The line info for markdown text.</param>
        /// <returns>Rendered text.</returns>
        StringBuffer Mark(SourceInfo sourceInfo, IMarkdownContext context = null);
        /// <summary>
        /// Mark markdown text.
        /// </summary>
        /// <param name="markdown">The markdown text.</param>
        /// <param name="file">The file of markdown.</param>
        /// <returns>Rendered text.</returns>
        string Markup(string markdown, string file = null);
    }
}
