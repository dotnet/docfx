// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Docfx.Build.ApiPage;
using Docfx.Build.Engine;
using Docfx.Build.ManagedReference;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Xunit.Sdk;

namespace docfx.Tests;

public partial class YamlSerializationTest
{
    [Theory]
    [TestData<XRefMap>]
    public void YamlSerializationTest_XRefMap(string path)
    {
        // Arrange
        var model = TestData.Load<XRefMap>(path);

        // Act/Assert
        ValidateYamlRoundTrip(model);
        ValidateYamlJsonRoundTrip(model);
    }
}
