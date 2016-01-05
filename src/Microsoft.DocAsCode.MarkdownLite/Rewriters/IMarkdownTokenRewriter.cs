// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// rewrite the markdown when rendering
    /// </summary>
    public interface IMarkdownTokenRewriter
    {
        /// <summary>
        /// rewrite
        /// </summary>
        /// <param name="engine">the engine</param>
        /// <param name="token">the token</param>
        /// <returns>If need rewrite, return the new token, otherwise, null</returns>
        IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token);
    }
}
