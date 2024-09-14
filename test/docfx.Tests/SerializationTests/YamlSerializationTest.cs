// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Docfx.Common;
using Docfx.Tests;
using Docfx.YamlSerialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using YamlDotNet.Serialization;
namespace docfx.Tests;

public partial class YamlSerializationTest
{
    private static readonly ThreadLocal<YamlSerializer> YamlJsonSerializer = new ThreadLocal<YamlSerializer>(() => new YamlSerializer(SerializationOptions.JsonCompatible | SerializationOptions.DisableAliases));

    /// <summary>
    /// Helper method to validate serialize/deserialize results.
    /// </summary>
    protected void ValidateYamlRoundTrip<T>(T model)
    {
        // Act
        using var writer = new StringWriter();
        YamlUtility.Serialize(writer, model);
        var yaml = writer.ToString();

        var result = YamlUtility.Deserialize<T>(new StringReader(yaml));

        // Assert
        result.Should().BeEquivalentTo(model);
    }

    /// <summary>
    /// Helper method to validate serialize/deserialize results.
    /// </summary>
    protected void ValidateYamlJsonRoundTrip<T>(T model)
    {
        // 1. Serialize to JSON with YamlDotNet
        using var writer = new StringWriter();
        YamlJsonSerializer.Value.Serialize(writer, model);
        var json = writer.ToString();

        // 2. Deserialize JSON to model
        var result = JsonUtility.Deserialize<T>(new StringReader(json));

        // Assert
        result.Should().BeEquivalentTo(model);
    }
}
