// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<MetadataJsonConfig>]
    public void JsonSerializationTest_MetadataJsonConfig(string path)
    {
        // Arrange
        var model = TestData.Load<MetadataJsonConfig>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);
    }
}
