// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using FluentAssertions;
using Markdig.Extensions.MediaLinks;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<MarkdownServiceProperties>]
    public void JsonSerializationTest_MarkdownServiceProperties(string path)
    {
        // Arrange
        var model = TestData.Load<MarkdownServiceProperties>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);

        // Additional test to validate deserialized result.
        var medialinksSettings = model.MarkdigExtensions.First(x => x.Name == "MediaLinks");
        var options = medialinksSettings.GetOptions(fallbackValue: new MediaOptions());
        options.Should().BeEquivalentTo(new MediaOptions
        {
            Width = "800",
            Height = "400",
        });
    }
}
