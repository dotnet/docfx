// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JsonSchemaTest
    {
        // Test against a subset of JSON schema test suite: https://github.com/json-schema-org/JSON-Schema-Test-Suite
        private static readonly string[] s_notSupportedSuites =
        {
            "additionalItems",
            "additionalProperties",
            "allOf",
            "anyOf",
            "boolean_schema",
            "const",
            "contains",
            "definitions",
            "dependencies",
            "exclusiveMaximum",
            "exclusiveMinimum",
            "if-then-else",
            "maximum",
            "maxItems",
            "maxLength",
            "maxProperties",
            "minimum",
            "minItems",
            "minLength",
            "minProperties",
            "multipleOf",
            "not",
            "oneOf",
            "pattern",
            "patternProperties",
            "propertyNames",
            "ref",
            "refRemote",
            "uniqueItems"
        };

        private static readonly string[] s_notSupportedTests =
        {
            "heterogeneous enum validation",
            "an array of schemas for items",
            "items and subitems",
            "with boolean schema",
            "patternProperties",
        };

        public static TheoryData<string, string, string> GetJsonSchemaTestSuite()
        {
            var result = new TheoryData<string, string, string>();
            foreach (var file in Directory.GetFiles("data/jschema/draft7"))
            {
                var suite = Path.GetFileNameWithoutExtension(file);
                if (s_notSupportedSuites.Contains(suite))
                {
                    continue;
                }

                foreach (var schema in JArray.Parse(File.ReadAllText(file)))
                {
                    var schemaText = schema["schema"].ToString();
                    foreach (var test in schema["tests"])
                    {
                        var description = $"{(schema["description"])}/{(test["description"])}";
                        if (s_notSupportedTests.Any(text => description.Contains(text)))
                        {
                            continue;
                        }
                        result.Add($"{suite}/{description}", schemaText, test.ToString());
                    }
                }
            }
            return result;
        }

        [Theory]
        [MemberData(nameof(GetJsonSchemaTestSuite))]
        public void TestJsonSchemaConfirmance(string description, string schemaText, string testText)
        {
            var schema = JsonConvert.DeserializeObject<JsonSchema>(schemaText);
            var test = JObject.Parse(testText);
            var errors = JsonSchemaValidation.Validate(schema, test["data"]);

            Assert.True(test.Value<bool>("valid") == (errors.Count == 0), description);
        }

        [Theory]

        // type validation
        [InlineData("{'type': 'boolean'}", "true", "")]
        [InlineData("{'type': 'array'}", "[]", "")]
        [InlineData("{'type': 'object'}", "{}", "")]
        [InlineData("{'type': 'string'}", "'text'", "")]
        [InlineData("{'type': 'integer'}", "123", "")]
        [InlineData("{'type': 'number'}", "123.456", "")]
        [InlineData("{'type': 'number'}", "123", "")]
        [InlineData("{'type': 'boolean'}", "'string'",
            "['error','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,8]")]
        [InlineData("{'type': 'object'}", "1",
            "['error','unexpected-type','Expect type 'Object' but got 'Integer'','file',1,1]")]
        [InlineData("{'type': 'string'}", "1",
            "['error','unexpected-type','Expect type 'String' but got 'Integer'','file',1,1]")]

        // union type validation
        [InlineData("{'type': ['string', 'null']}", "'a'", "")]
        [InlineData("{'properties': {'a': {'type': ['string', 'null']}}}", "{'a': null}", "")]
        [InlineData("{'type': ['string', 'null']}", "1",
            "['error','unexpected-type','Expect type 'String, Null' but got 'Integer'','file',1,1]")]

        // enum validation
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'a'", "")]
        [InlineData("{'type': 'string', 'enum': []}", "'unknown'",
            "['error','undefined-value','Value 'unknown' is not accepted. Valid values: ','file',1,9]")]
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'unknown'",
            "['error','undefined-value','Value 'unknown' is not accepted. Valid values: 'a', 'b'','file',1,9]")]

        [InlineData("{'type': 'number', 'enum': [1, 2]}", "1", "")]
        [InlineData("{'type': 'number', 'enum': [1, 2]}", "3",
            "['error','undefined-value','Value '3' is not accepted. Valid values: '1', '2'','file',1,1]")]

        // properties validation
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 'value'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 1}",
            "['error','unexpected-type','Expect type 'String' but got 'Integer'','file',1,9]")]

        // array validation
        [InlineData("{'items': {'type': 'string'}}", "['a','b']", "")]
        [InlineData("{'items': {'type': 'boolean'}}", "['a','b']",
            @"['error','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,4]
              ['error','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,8]")]

        // required validation
        [InlineData("{'required': []}", "{}", "")]
        [InlineData("{'required': ['a']}", "{'a': 1}", "")]
        [InlineData("{'required': ['a']}", "{'b': 1}",
            "['error','field-required','Missing required field 'a'','file',1,1]")]
        public void TestJsonSchemaValidation(string schema, string json, string expectedErrors)
        {
            var errors = JsonSchemaValidation.Validate(
                JsonUtility.Deserialize<JsonSchema>(schema.Replace('\'', '"')),
                JsonUtility.Parse(json.Replace('\'', '"'), "file").Item2);

            var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
            var actual = string.Join('\n', errors.Select(err => err.ToString()).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }
    }
}
