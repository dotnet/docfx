// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// Markdown rewritable (for object contains <see cref="IMarkdownToken"/>).
    /// </summary>
    /// <typeparam name="T">The type of implement this interface.</typeparam>
    public interface IMarkdownRewritable<out T>
    {
        /// <summary>
        /// Rewrite object with <see cref="IMarkdownRewriteEngine"/>
        /// </summary>
        /// <param name="rewriteEngine">The rewrite engine</param>
        /// <returns>The rewritten object.</returns>
        T Rewrite(IMarkdownRewriteEngine rewriteEngine);
    }
}