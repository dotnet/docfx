// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Helpers;
using Markdig.Parsers;

namespace Docfx.MarkdigEngine.Extensions;

public class InclusionBlockParser : BlockParser
{
    private const string StartString = "[!include";

    public InclusionBlockParser()
    {
        OpeningCharacters = ['['];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        // [!include[<title>](<filepath>)]
        var column = processor.Column;
        var line = processor.Line;
        var command = line.ToString();

        if (!ExtensionsHelper.MatchStart(ref line, StartString, false))
        {
            return BlockState.None;
        }
        else
        {
            if (line.CurrentChar == '+')
            {
                line.NextChar();
            }
        }

        string title = null, path = null;

        if (!ExtensionsHelper.MatchLink(ref line, ref title, ref path) || !ExtensionsHelper.MatchInclusionEnd(ref line))
        {
            return BlockState.None;
        }

        while (line.CurrentChar.IsSpaceOrTab()) line.NextChar();
        if (line.CurrentChar != '\0')
        {
            return BlockState.None;
        }

        processor.NewBlocks.Push(new InclusionBlock(this)
        {
            Title = title,
            IncludedFilePath = path,
            Line = processor.LineIndex,
            Column = column,
        });

        return BlockState.BreakDiscard;
    }
}
