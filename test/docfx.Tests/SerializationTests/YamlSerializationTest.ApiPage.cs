// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Build.ApiPage;
using Docfx.Common;
using Docfx.Tests;
using FluentAssertions;
using YamlDotNet.Serialization;

namespace docfx.Tests;

public partial class YamlSerializationTest
{
    [Theory]
    [TestData<ApiPage>]
    public void YamlSerializationTest_ApiPage(string path)
    {
        // Arrange
        var model = LoadApiPage(path); // Note: Loading ApiPage YAML file requires custom logics.

        // Act/Assert
        ValidateApiPageRoundTrip(model);
    }

    /// <summary>
    /// 
    /// </summary>
    private static ApiPage LoadApiPage(string path)
    {
        path = PathHelper.ResolveTestDataPath(path);

        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization()
                                                    .Build();

        // 1. Deserialize ApiPage yaml as Dictionary
        // 2. Serialize to json
        // 3. Deserialize as ApiPage instance
        var model = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(path));
        var json = JsonSerializer.Serialize(model);
        return JsonSerializer.Deserialize<ApiPage>(json, ApiPage.JsonSerializerOptions);
    }

    private static void ValidateApiPageRoundTrip(ApiPage model)
    {
        // Act
        var yaml = ToYaml(model);
        var result = ToApiPage(yaml);

        // Assert

        // ApiPage model can't be validated with `BeEquivalentTo` (See: https://github.com/dotnet/docfx/pull/10232)
        // So compare serialized YAML contents.
        ToYaml(result).Should().Be(yaml);
    }

    private static string ToYaml(ApiPage model)
    {
        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();

        var json = JsonSerializer.Serialize(model, Docfx.Build.ApiPage.ApiPage.JsonSerializerOptions);
        var obj = deserializer.Deserialize(json);

        using var writer = new StringWriter();
        YamlUtility.Serialize(writer, obj, "YamlMime:ApiPage");
        return writer.ToString();
    }

    private static ApiPage ToApiPage(string yaml)
    {
        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
        var dict = deserializer.Deserialize<Dictionary<object, object>>(new StringReader(yaml));
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<ApiPage>(json, ApiPage.JsonSerializerOptions);
    }
}
