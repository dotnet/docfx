// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<Manifest>]
    public void JsonSerializationTest_Manifest(string path)
    {
        // Arrange
        var model = TestData.Load<Manifest>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);
    }
}
