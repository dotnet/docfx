// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Extensions.EmphasisExtras;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="EmphasisExtraExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/EmphasisExtraSpecs.md"/>
[Trait("Related", "MarkdigExtension")]
public class EmphasisExtraTest
{
    [Fact]
    public void EmphasisExtraTest_DocfxDefault()
    {
        var content = "The following text ~~is deleted~~";
        var expected = "<p>The following text <del>is deleted</del></p>";

        // `Strikethrough` is enabled by default.
        TestUtility.VerifyMarkup(content, expected);
    }

    [Fact]
    public void EmphasisExtraTest_ResetToMarkdigDefault()
    {
        var content =
            """
            The following text ~~is deleted~~
            H~2~O is a liquid. 2^10^ is 1024
            ++Inserted text++
            ==Marked text==
            """;

        var expected =
            """
            <p>
            The following text <del>is deleted</del>
            H<sub>2</sub>O is a liquid. 2<sup>10</sup> is 1024
            <ins>Inserted text</ins>
            <mark>Marked text</mark>
            </p>
            """;

        // `EmphasisExtraOptions.Default` option is used
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["EmphasisExtras"]);
    }

    [Fact]
    public void EmphasisExtraTest_SuperscriptAndSubscript()
    {
        var content = "H~2~O is a liquid. 2^10^ is 1024";

        // `Superscript` and `Subscript` are disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        // `Superscript` and `Subscript` is enabled when using default options or option is explicitly specified.
        {
            var expected = "<p>H<sub>2</sub>O is a liquid. 2<sup>10</sup> is 1024</p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
              new("EmphasisExtras", "Superscript, Subscript")]);
        }
    }

    [Fact]
    public void EmphasisExtraTest_Inserted()
    {
        var content = "++Inserted text++";

        // `Inserted` is disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        // `Inserted` is enabled when using default options or option is explicitly specified.
        {
            var expected = "<p><ins>Inserted text</ins></p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
              new("EmphasisExtras", "Inserted")]);
        }
    }

    [Fact]
    public void EmphasisExtraTest_Marked()
    {
        var content = "==Marked text==";

        // `Marked` is disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        // `Marked` is enabled when using default options or option is explicitly specified.
        {
            var expected = "<p><mark>Marked text</mark></p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
              new("EmphasisExtras", "Marked")]);
        }
    }
}
