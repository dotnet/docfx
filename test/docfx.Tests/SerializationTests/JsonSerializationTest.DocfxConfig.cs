// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<DocfxConfig>]
    public void JsonSerializationTest_DocfxConfig(string path)
    {
        // Arrange
        var model = TestData.Load<DocfxConfig>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);
    }
}
