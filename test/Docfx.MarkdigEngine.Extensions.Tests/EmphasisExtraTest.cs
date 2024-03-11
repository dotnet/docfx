// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class EmphasisExtraTest
{
    [Fact]
    public void EmphasisExtraTest_Strikethrough()
    {
        var content = @"The following text ~~is deleted~~";
        var expected = @"<p>The following text <del>is deleted</del></p>";

        // `Strikethrough` is enabled by default.
        TestUtility.VerifyMarkup(content, expected);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["EmphasisExtras"]);
    }

    [Fact]
    public void EmphasisExtraTest_SuperscriptAndSubscript()
    {
        var content = @"H~2~O is a liquid. 2^10^ is 1024";

        // `Superscript` and `Subscript` are disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        {
            var expected = @"<p>H<sub>2</sub>O is a liquid. 2<sup>10</sup> is 1024</p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["EmphasisExtras"]);
        }
    }

    [Fact]
    public void EmphasisExtraTest_Inserted()
    {
        var content = @"++Inserted text++";

        // `Inserted` is disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        {
            var expected = @"<p><ins>Inserted text</ins></p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["EmphasisExtras"]);
        }
    }

    [Fact]
    public void EmphasisExtraTest_Marked()
    {
        var content = @"==Marked text==";

        // `Marked` is disabled by default.
        {
            var expected = $"<p>{content}</p>";
            TestUtility.VerifyMarkup(content, expected);
        }
        {
            var expected = @"<p><mark>Marked text</mark></p>";
            TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["EmphasisExtras"]);
        }

    }
}
