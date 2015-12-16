// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public interface IMarkdownParser
    {
        IMarkdownContext Context { get; }
        Dictionary<string, LinkObj> Links { get; }
        Options Options { get; }

        IMarkdownContext SwitchContext(IMarkdownContext context);
        ImmutableArray<IMarkdownToken> Tokenize(string markdown);
    }
}