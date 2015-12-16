// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IMarkdownEngine
    {
        IMarkdownParser Parser { get; }
        IMarkdownRenderer Renderer { get; }
        // todo : coming soon.
        object RewriterEngine { get; }

        StringBuffer Mark(string markdown, IMarkdownContext context = null);
        string Markup(string markdown);
    }
}