// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class InclusionInlineParser : InlineParser
{
    private const string StartString = "[!include";

    public InclusionInlineParser()
    {
        OpeningCharacters = new[] { '[' };
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var startPosition = processor.GetSourcePosition(slice.Start, out var line, out var column);

        if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
        {
            return false;
        }

        if (slice.CurrentChar == '-')
        {
            slice.NextChar();
        }

        string title = null, path = null;

        if (!ExtensionsHelper.MatchLink(ref slice, ref title, ref path) || !ExtensionsHelper.MatchInclusionEnd(ref slice))
        {
            return false;
        }

        processor.Inline = new InclusionInline
        {
            Title = title,
            IncludedFilePath = path,
            Line = line,
            Column = column,
            Span = new SourceSpan(startPosition, processor.GetSourcePosition(slice.Start - 1)),
            IsClosed = true,
        };

        return true;
    }
}
