// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;

namespace docfx.Tests;

public partial class YamlSerializationTest
{
    [Theory]
    [TestData<TocItemViewModel>]
    public void YamlSerializationTest_TocItemViewModel(string path)
    {
        // Arrange
        var model = TestData.Load<TocItemViewModel>(path);

        // Act/Assert
        ValidateYamlRoundTrip(model);
        ValidateYamlJsonRoundTrip(model);
    }
}
