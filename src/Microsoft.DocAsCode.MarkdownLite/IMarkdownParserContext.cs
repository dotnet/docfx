// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IMarkdownParserContext
    {
        string CurrentMarkdown { get; }

        LineInfo LineInfo { get; }

        void Consume(int charCount);
    }
}
