// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

public class OpsMetadataRuleConverterTest
{
    [Fact]
    public void TestGenerateJsonSchema()
    {
        var rules = File.ReadAllText("data/validation/rulesets.json");
        var allowlists = File.ReadAllText("data/validation/taxonomy-allowlists.json");
        var expectedSchema = File.ReadAllText("data/validation/schema.json");
        var actualSchema = OpsMetadataRuleConverter.GenerateJsonSchema(rules, allowlists, new ErrorList());

        new JsonDiff().Verify(JObject.Parse(expectedSchema), JObject.Parse(actualSchema));
    }
}
