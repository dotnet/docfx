﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IMarkdownParsingContext
    {
        string CurrentMarkdown { get; }
        int LineNumber { get; }
        string File { get; }
        bool IsInParagraph { get; set; }

        SourceInfo Consume(int charCount);
    }
}
