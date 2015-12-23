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
        /// <param name="source">The source of markdown text.</param>
        /// <returns>If matched, an instance of <see cref="IMarkdownToken"/> should be return, otherwise null.</returns>
        IMarkdownToken TryMatch(IMarkdownParser parser, ref string source);
    }
}
