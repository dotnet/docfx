// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;

using Newtonsoft.Json.Linq;

namespace Docfx.Common.Tests;

[TestProperty("Related", "JObjectMerger")]
[TestClass]
public class JObjectMergerTest
{
    [TestMethod]
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
        Assert.IsNotNull(sourceJObj);
        Assert.AreEqual("Overrides1", sourceJObj["StringKey"]);
        Assert.AreEqual("Source2", sourceJObj["AdditionalSourceKey"]);
        Assert.AreEqual("Overrides2", sourceJObj["AdditionalOverridesKey"]);
        Assert.AreEqual("Overrides3", sourceJObj["JObjectValue"]["CommonKey"]);
        Assert.AreEqual("Overrides4", sourceJObj["JObjectValue"]["AdditionalOverridesKey"]);
    }
}
