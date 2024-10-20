// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<TocItemViewModel>]
    public void JsonSerializationTest_TocItemViewModel(string path)
    {
        // Arrange
        var model = TestData.Load<TocItemViewModel>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);
    }
}
