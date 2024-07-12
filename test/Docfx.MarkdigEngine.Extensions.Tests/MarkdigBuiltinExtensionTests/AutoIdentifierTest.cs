// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Extensions.AutoIdentifiers;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="AutoIdentifierExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/AutoIdentifierSpecs.md"/>
[Trait("Related", "MarkdigExtension")]
public class AutoIdentifierTest
{
    [Fact]
    public void AutoIdentifierTest_DocfxDefault()
    {
        // docfx use `AutoIdentifierOptions.GitHub` as default options.
        var content = "# This - is a &@! heading _ with . and ! -";
        var expected = @"<h1 id=""this---is-a--heading-_-with--and---"">This - is a &amp;@! heading _ with . and ! -</h1>";

        TestUtility.VerifyMarkup(content, expected);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
           new("AutoIdentifiers", "GitHub")
        ]);
    }

    [Fact]
    public void AutoIdentifierTest_MarkdigDefault()
    {
        var content = "# This - is a &@! heading _ with . and ! -";
        var expected = @"<h1 id=""this-is-a-heading_with.and"">This - is a &amp;@! heading _ with . and ! -</h1>";

        // Default option is used when 
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["AutoIdentifiers"]);

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
           new("AutoIdentifiers", "Default")
        ]);

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
           new("AutoIdentifiers", "AutoLink, AllowOnlyAscii")
        ]);
    }

    [Fact]
    public void AutoIdentifierTest_None()
    {
        var content = "# This - is a &@! heading _ with . and ! -";
        var expected = @"<h1 id=""this-is-a-heading_with.and"">This - is a &amp;@! heading _ with . and ! -</h1>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
           new("AutoIdentifiers", "None")
        ]);
    }
}
