// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Docfx;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Dotnet;
using Docfx.Plugins;
using FluentAssertions;

namespace docfx.Tests;

public partial class YamlSerializationTest
{
    [Theory]
    [TestData<ConfigFilterRule>]
    public void YamlSerializationTest_FilterConfig(string path)
    {
        // Arrange
        var model = TestData.Load<ConfigFilterRule>(path);

        // Act/Assert
        ValidateYamlRoundTrip(model);

        // ConfigFilterRule don't support JSON serialization/deserialization
        // ValidateYamlJsonRoundTrip(model);
    }
}
