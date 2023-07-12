// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;

using Newtonsoft.Json.Linq;

using Xunit;

namespace Docfx.Common.Tests;

[Trait("Related", "JObjectMerger")]
public class JObjectMergerTest
{
    [Fact]
    public void TestJObjectMergerWithBasicScenarios()
    {
        object source = new JObject
        {
            { "StringKey", "Source1" },
            { "AdditionalSourceKey", "Source2" },
            {
                "JObjectValue", JObject.FromObject(new Dictionary<string, object>
                {
                    { "CommonKey", "Source3" },
                })
            },
        };
        object overrides = new Dictionary<object, object>
        {
            { "StringKey", "Overrides1" },
            { "AdditionalOverridesKey", "Overrides2" },
            {
                "JObjectValue", new Dictionary<string, object>
                {
                    { "CommonKey", "Overrides3" },
                    { "AdditionalOverridesKey", "Overrides4" },
                }
            },
        };
        new MergerFacade(
            new JObjectMerger(
                new ReflectionEntityMerger()))
                .Merge(ref source, overrides);
        var sourceJObj = source as JObject;
        Assert.NotNull(sourceJObj);
        Assert.Equal("Overrides1", sourceJObj["StringKey"]);
        Assert.Equal("Source2", sourceJObj["AdditionalSourceKey"]);
        Assert.Equal("Overrides2", sourceJObj["AdditionalOverridesKey"]);
        Assert.Equal("Overrides3", sourceJObj["JObjectValue"]["CommonKey"]);
        Assert.Equal("Overrides4", sourceJObj["JObjectValue"]["AdditionalOverridesKey"]);
    }
}
