// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using Docfx.Plugins;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

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
            Redirections = new List<XRefMapRedirection>
            {
                new XRefMapRedirection
                {
                    Href = "Dummy",
                    UidPrefix = "Dummy"
                },
            },
            References = new List<XRefSpec>
            {
                new XRefSpec(new Dictionary<string,object>
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
            },
            Others = new Dictionary<string, object>
            {
                ["Other1"] = "Dummy",
            }
        };

        // Arrange
        var jsonResult = RoundtripByNewtonsoftJson(model);
        var yamlResult = RoundtripWithYamlDotNet(model);

        // Assert
        jsonResult.Should().BeEquivalentTo(model);
        yamlResult.Should().BeEquivalentTo(model);
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
