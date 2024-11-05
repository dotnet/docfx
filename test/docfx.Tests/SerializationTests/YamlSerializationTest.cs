// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Common;
using Docfx.YamlSerialization;
using FluentAssertions;
using FluentAssertions.Equivalency;
namespace docfx.Tests;

public partial class YamlSerializationTest
{
    private static readonly ThreadLocal<YamlSerializer> YamlJsonSerializer = new(() => new YamlSerializer(SerializationOptions.JsonCompatible | SerializationOptions.DisableAliases));

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

        // 2. Deserialize JSON to models
        var systemTextJsonModel = SystemTextJsonUtility.Deserialize<T>(json);
        var newtownsoftJsonModel = NewtonsoftJsonUtility.Deserialize<T>(new StringReader(json));

        // Assert

        // 3.1. Validate SystemTextJson/NewtonsoftJson models.
        systemTextJsonModel.Should().BeEquivalentTo(newtownsoftJsonModel, customAssertionOptions);
        newtownsoftJsonModel.Should().BeEquivalentTo(systemTextJsonModel, customAssertionOptions);

        // 3.3. Validate models that is loaded by YamlUtility.
        model.Should().BeEquivalentTo(newtownsoftJsonModel, customAssertionOptions);
        model.Should().BeEquivalentTo(systemTextJsonModel, customAssertionOptions);
        systemTextJsonModel.Should().BeEquivalentTo(model, customAssertionOptions);
        newtownsoftJsonModel.Should().BeEquivalentTo(systemTextJsonModel, customAssertionOptions);
    }

    private static EquivalencyAssertionOptions<T> customAssertionOptions<T>(EquivalencyAssertionOptions<T> opt)
    {
        // By default. JsonElement is compared by reference because JsonElement don't override Equals.
        return opt.ComparingByMembers<JsonElement>()
                  .Using(new CustomEqualityEquivalencyStep());
    }
}
