﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;
using FluentAssertions;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    [Theory]
    [TestData<XRefMap>]
    public void JsonSerializationTest_XRefMap(string path)
    {
        // Arrange
        var model = TestData.Load<XRefMap>(path);

        // Act/Assert
        ValidateJsonRoundTrip(model);
    }
}
