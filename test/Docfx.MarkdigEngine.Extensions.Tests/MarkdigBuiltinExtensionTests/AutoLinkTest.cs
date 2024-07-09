// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.MarkdigEngine.Extensions;
using Markdig.Extensions.AutoLinks;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="AutoLinkExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/AutoLinks.md"/>
[Trait("Related", "MarkdigExtension")]
public class AutoLinkTest
{
    [Fact]
    public void AutoLinkTest_DocfxDefault()
    {
        // docfx use `AutoIdentifierOptions.GitHub` as default options.
        var content = "This is not a nhttp://www.google.com URL but this is (https://www.google.com)";
        var expected = "<p>This is not a nhttp://www.google.com URL but this is (<a href=\"https://www.google.com\">https://www.google.com</a>)</p>";

        TestUtility.VerifyMarkup(content, expected);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["AutoLinks"]);
    }

    [Fact]
    public void AutoLinkTest_Custom()
    {
        var options = new AutoLinkOptions
        {
            OpenInNewWindow = true,           // Add `target="_blank"` attribute to generated link.
            UseHttpsForWWWLinks = false,      // Default: false.
            ValidPreviousCharacters = "*_~("  // Default: *_~("
        };

        var content = "Sample URL (http://www.google.com)";
        var expected = @"<p>Sample URL (<a href=""http://www.google.com"" target=""_blank"">http://www.google.com</a>)</p>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
           new("AutoLinks", JsonSerializer.SerializeToNode(options, MarkdigExtensionSettingConverter.DefaultSerializerOptions))
        ]);
    }
}
