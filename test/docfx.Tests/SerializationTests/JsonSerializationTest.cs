// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Common;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace docfx.Tests;

public partial class JsonSerializationTest
{
    /// <summary>
    /// Helper method to validate serialize/deserialize results.
    /// </summary>
    protected void ValidateJsonRoundTrip<T>(T model)
    {
        // 1. Validate serialized result.
        var newtonsoftJson = NewtonsoftJsonUtility.Serialize(model);
        var systemTextJson = SystemTextJsonUtility.Serialize(model);
        systemTextJson.Should().Be(newtonsoftJson);

        // 2. Validate deserialized result.
        var json = systemTextJson;
        var systemTextJsonModel = SystemTextJsonUtility.Deserialize<T>(json);
        var newtonsoftJsonModel = NewtonsoftJsonUtility.Deserialize<T>(new StringReader(json));
        systemTextJsonModel.Should().BeEquivalentTo(model, customAssertionOptions);
        newtonsoftJsonModel.Should().BeEquivalentTo(model, customAssertionOptions);

        // 3. Validate JsonUtility roundtrip result.
        json = JsonUtility.Serialize(model);
        var result = JsonUtility.Deserialize<T>(new StringReader(json));
        result.Should().BeEquivalentTo(model, customAssertionOptions);
    }

    private static EquivalencyAssertionOptions<T> customAssertionOptions<T>(EquivalencyAssertionOptions<T> opt)
    {
        // By default. JsonElement is compared by reference because JsonElement don't override Equals.
        return opt.ComparingByMembers<JsonElement>()
                  .Using(new CustomEqualityEquivalencyStep());
    }
}
