// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public interface IMarkdownParsingContext
    {
        string Markdown { get; }
        string CurrentMarkdown { get; }
        int LineNumber { get; }
        string File { get; }
        bool IsInParagraph { get; set; }

        SourceInfo Consume(int charCount);
        MatchResult Match(Matcher matcher);
    }
}
