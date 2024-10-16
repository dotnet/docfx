// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Docfx.DataContracts.ManagedReference;
using FluentAssertions;

namespace docfx.Tests;

public partial class YamlSerializationTest
{
    [Theory]
    [TestData<PageViewModel>]
    public void YamlSerializationTest_ManagedReference(string path)
    {
        // Arrange
        var model = TestData.Load<PageViewModel>(path);

        // Act/Assert
        ValidateYamlRoundTrip(model);
        ValidateYamlJsonRoundTrip(model);
    }
}
