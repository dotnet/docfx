// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    /// <summary>
    /// Markdown renderer.
    /// </summary>
    public interface IMarkdownRenderer
    {
        /// <summary>
        /// Get the markdown engine.
        /// </summary>
        IMarkdownEngine Engine { get; }
        /// <summary>
        /// Get the No. links.
        /// </summary>
        Dictionary<string, LinkObj> Links { get; }
        /// <summary>
        /// Get the <see cref="Options"/>.
        /// </summary>
        Options Options { get; }

        /// <summary>
        /// Render a token.
        /// </summary>
        /// <param name="token">The token to render.</param>
        /// <returns>The text.</returns>
        StringBuffer Render(IMarkdownToken token);
    }
}