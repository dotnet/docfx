// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    public interface IDfmCustomizedRendererPart
    {
        string Name { get; }
        Type MarkdownRendererType { get; }
        Type MarkdownTokenType { get; }
        Type MarkdownContextType { get; }

        bool Match(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context);
        StringBuffer Render(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context);
    }
}
