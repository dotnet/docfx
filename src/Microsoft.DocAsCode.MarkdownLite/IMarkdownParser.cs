// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    /// Markdown parser.
    /// </summary>
    public interface IMarkdownParser
    {
        /// <summary>
        /// Get the current markdown context.
        /// </summary>
        IMarkdownContext Context { get; }
        /// <summary>
        /// Get the No. links.
        /// </summary>
        Dictionary<string, LinkObj> Links { get; }
        /// <summary>
        /// Get the <see cref="Options"/>.
        /// </summary>
        Options Options { get; }

        /// <summary>
        /// Switch the markdown context.
        /// </summary>
        /// <param name="context">New context.</param>
        /// <returns>The old context.</returns>
        IMarkdownContext SwitchContext(IMarkdownContext context);
        /// <summary>
        /// Tokenize the markdown text.
        /// </summary>
        /// <param name="markdown">The markdown text.</param>
        /// <returns>A list of <see cref="IMarkdownToken"/>.</returns>
        ImmutableArray<IMarkdownToken> Tokenize(SourceInfo sourceInfo);
    }
}