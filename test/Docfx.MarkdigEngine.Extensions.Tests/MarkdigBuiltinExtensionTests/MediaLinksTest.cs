// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.MarkdigEngine.Extensions;
using Markdig.Extensions.MediaLinks;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

/// <summary>
/// Unit tests for markdig <see cref="MediaLinkExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/MediaSpecs.md"/>
[Trait("Related", "MarkdigExtension")]
public class MediaLinksTest
{
    [Fact]
    public void MediaLinksTest_Default()
    {
        var content = "![static mp4](https://example.com/video.mp4)";

        var expected = """<p><video width="500" height="281" controls=""><source type="video/mp4" src="https://example.com/video.mp4"></source></video></p>""";

        TestUtility.VerifyMarkup(content, expected);
        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["MediaLinks"]);
    }

    [Fact]
    public void MediaLinksTest_Custom()
    {
        // `ExtensionToMimeType` and `Hosts` property override is not supported.
        // Because it getter-only property. and it can't deserialize property value);
        var options = new MediaOptions
        {
            Height = "100",              // Default: 500
            Width = "100",               // Default: 281
            AddControlsProperty = false, // Default: true
            Class = "custom-class",      // Default: ""
        };

        var content = "![static mp4](https://example.com/video.mp4)";
        var expected = $"""<p><video class="{options.Class}" width="{options.Width}" height="{options.Height}"><source type="video/mp4" src="https://example.com/video.mp4"></source></video></p>""";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
            new("MediaLinks", JsonSerializer.SerializeToNode(options, MarkdigExtensionSettingConverter.DefaultSerializerOptions))
        ]);
    }
}
