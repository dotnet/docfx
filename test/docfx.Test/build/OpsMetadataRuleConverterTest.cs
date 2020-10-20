// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build
{
    public class OpsMetadataRuleConverterTest
    {
        [Fact]
        public void TestGenerateJsonSchema()
        {
            var rules = File.ReadAllText("data/validation/rulesets.json");
            var allowlists = File.ReadAllText("data/validation/allowlists.json");
            var expectedSchema = File.ReadAllText("data/validation/schema.json");
            var actualSchema = OpsMetadataRuleConverter.GenerateJsonSchema(rules, allowlists);
            File.WriteAllText("data/validation/schema.json", actualSchema.ToString());

            new JsonDiff().Verify(JObject.Parse(expectedSchema), JObject.Parse(actualSchema));
        }
    }
}
