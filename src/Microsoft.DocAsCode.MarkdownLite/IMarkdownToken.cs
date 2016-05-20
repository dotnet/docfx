// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// The token for markdown.
    /// It should be immutable.
    /// </summary>
    public interface IMarkdownToken
    {
        /// <summary>
        /// The rule created this token.
        /// </summary>
        IMarkdownRule Rule { get; }

        /// <summary>
        /// The context when created this token.
        /// </summary>
        IMarkdownContext Context { get; }

        /// <summary>
        /// The raw markdown.
        /// </summary>
        string RawMarkdown { get; }

        /// <summary>
        /// The line info of this token.
        /// </summary>
        LineInfo LineInfo { get; }
    }
}
