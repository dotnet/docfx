// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx;
using Docfx.Common;
using Docfx.MarkdigEngine.Extensions;
using Docfx.Plugins;
using FluentAssertions;

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
    }
}
