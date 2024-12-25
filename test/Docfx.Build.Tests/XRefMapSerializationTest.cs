// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using FluentAssertions;
using Xunit;

namespace Docfx.Build.Engine.Tests;

public class XRefMapSerializationTest
{
    [Fact]
    public void XRefMapSerializationRoundTripTest()
    {
        var model = new XRefMap
        {
            BaseUrl = "http://localhost",
            Sorted = true,
            HrefUpdated = null,
            Redirections =
            [
                new()
                {
                    Href = "Dummy",
                    UidPrefix = "Dummy"
                },
            ],
            References =
            [
                new(new Dictionary<string,object>
                {
                    ["Additional1"] = "Dummy",
                })
                {
                    Uid =  "Dummy",
                    Name = "Dummy",
                    Href = "Dummy",
                    CommentId ="Dummy",
                    IsSpec = true,
                },
            ],
            Others = new Dictionary<string, object>
            {
                ["StringValue"] = "Dummy",
                ["BooleanValue"] = true,
                ["IntValue"] = int.MaxValue,
                ["LongValue"] = long.MaxValue,
                ["DoubleValue"] = 1.234d,

                //// YamlDotNet don't deserialize dictionary's null value.
                // ["NullValue"] = null,

                //// Following types has no deserialize compatibility (NewtonsoftJson deserialize value to JArray/Jvalue)
                // ["ArrayValue"] = new object[] { 1, 2, 3 },
                // ["ObjectValue"] = new Dictionary<string, string>{["Prop1"="Dummy"]}
            }
        };

        // Validate serialized JSON text.
        {
            // Arrange
            var systemTextJson = SystemTextJsonUtility.Serialize(model);
            var newtonsoftJson = JsonUtility.Serialize(model);

            // Assert
            systemTextJson.Should().Be(newtonsoftJson);
        }

        // Validate roundtrip result.
        {
            // Arrange
            var systemTextJsonResult = RoundtripBySystemTextJson(model);
            var newtonsoftJsonResult = RoundtripByNewtonsoftJson(model);
            var yamlResult = RoundtripWithYamlDotNet(model);

            // Assert
            systemTextJsonResult.Should().BeEquivalentTo(model);
            newtonsoftJsonResult.Should().BeEquivalentTo(model);
            yamlResult.Should().BeEquivalentTo(model);
        }
    }

    private static T RoundtripBySystemTextJson<T>(T model)
    {
        var json = SystemTextJsonUtility.Serialize(model);
        return SystemTextJsonUtility.Deserialize<T>(json);
    }

    private static T RoundtripByNewtonsoftJson<T>(T model)
    {
        var json = JsonUtility.Serialize(model);
        return JsonUtility.Deserialize<T>(new StringReader(json));
    }

    private static T RoundtripWithYamlDotNet<T>(T model)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        YamlUtility.Serialize(sw, model);
        var json = sb.ToString();
        return YamlUtility.Deserialize<T>(new StringReader(json));
    }
}
