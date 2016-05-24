// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// Markdown rule.
    /// </summary>
    public interface IMarkdownRule
    {
        /// <summary>
        /// Get the name of rule.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Try match this rule.
        /// </summary>
        /// <param name="parser">The markdown parser.</param>
        /// <param name="context">The context for parser, contains markdown text, line number and file.</param>
        /// <returns>If matched, an instance of <see cref="IMarkdownToken"/> should be return, otherwise null.</returns>
        IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context);
    }
}
