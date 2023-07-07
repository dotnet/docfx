// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Docfx.Plugins.Tests;

public class JsonConverterTest
{
    [Fact]
    [Trait("Related", "docfx")]
    public void TestJObjectDictionaryToObjectDictionaryConverterSerializeAndDeserialize()
    {
        string jsonString = @"
{
    ""baseUrl"": ""https://dotnet.github.io/docfx"",
    ""priority"": 0.5,
    ""changefreq"": ""monthly"",
    ""fileOptions"":{
        ""**/api/**.yml"": {
            ""priority"": 0.3
        },
        ""**.md"": {
            ""baseUrl"": ""https://dotnet.github.io/docfx/1"",
            ""priority"": 0.8
        }
    }
}
";
        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() },
            }
        };

        var buildOptions = JsonConvert.DeserializeObject<SitemapOptions>(jsonString, settings);
        Assert.Equal(2, buildOptions.FileOptions.Count);
        Assert.Equal(0.3, buildOptions.FileOptions[0].Value.Priority);
        Assert.Equal(PageChangeFrequency.Monthly, buildOptions.ChangeFrequency);
        Assert.Null(buildOptions.FileOptions[0].Value.ChangeFrequency);
        Assert.Equal("https://dotnet.github.io/docfx/1", buildOptions.FileOptions[1].Value.BaseUrl);

        var convertedObject = JsonConvert.DeserializeObject<SitemapOptions>(JsonConvert.SerializeObject(buildOptions, settings), settings);

        Assert.Equal(2, convertedObject.FileOptions.Count);
        Assert.Equal(0.3, convertedObject.FileOptions[0].Value.Priority);
        Assert.Equal(PageChangeFrequency.Monthly, convertedObject.ChangeFrequency);
        Assert.Null(convertedObject.FileOptions[0].Value.ChangeFrequency);
        Assert.Equal("https://dotnet.github.io/docfx/1", convertedObject.FileOptions[1].Value.BaseUrl);
    }
}
