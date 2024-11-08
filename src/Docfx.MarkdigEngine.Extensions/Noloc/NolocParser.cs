// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Helpers;
using Markdig.Parsers;

namespace Docfx.MarkdigEngine.Extensions;

public class NolocParser : InlineParser
{
    // syntax => :::no-loc text="{content}":::
    private const string StartString = ":::no-loc text=\"";
    private const string EndString = "\":::";

    public NolocParser()
    {
        OpeningCharacters = [':'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        if (!ExtensionsHelper.MatchStart(ref slice, StartString, true))
        {
            return false;
        }

        var text = ExtensionsHelper.TryGetStringBeforeChars(['\"', '\n'], ref slice);

        if (text == null || text.Contains('\n'))
        {
            return false;
        }

        if (!ExtensionsHelper.MatchStart(ref slice, EndString, true))
        {
            return false;
        }

        processor.Inline = new NolocInline
        {
            Text = text
        };

        return true;
    }
}
