// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Extensions.Emoji;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="EmojiExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/EmojiSpecs.md"/>
[Trait("Related", "MarkdigExtension")]
public class EmojiTest
{
    [Fact]
    public void EmojiTest_DocfxDefault()
    {
        var content = "**content :** :smile:";
        var expected = @"<p><strong>content :</strong> 😄</p>";

        // By default. `UseEmojiAndSmiley(enableSmileys: false)` option used.
        TestUtility.VerifyMarkup(content, expected);
    }

    [Fact]
    public void EmojiTest_MarkdigDefault()
    {
        var content = ":)";
        var expected = @"<p>😃</p>";

        // `UseEmojiAndSmiley(enableSmileys: true)` option is used when enable `Emojis` extension by name. 
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["Emojis"]);
    }

    [Fact]
    public void EmojiTest_Smileys_Enabled()
    {
        var content = ":)";
        var expected = @"<p>😃</p>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
            new("Emojis", "DefaultAndSmileys"),
        ]);
    }

    [Fact]
    public void EmojiTest_Smileys_Disabled()
    {
        var content = ":)";
        var expected = "<p>:)</p>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
            new("Emojis", "Default"),
        ]);
    }
}
