// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    /// <summary>
    /// Null object.
    /// </summary>
    internal sealed class MarkdownNullTokenRewriter : IMarkdownTokenRewriter
    {
        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            return null;
        }
    }
}
