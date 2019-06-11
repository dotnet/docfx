// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
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
            "allOf",
            "anyOf",
            "boolean_schema",
            "const",
            "contains",
            "definitions",
            "exclusiveMaximum",
            "exclusiveMinimum",
            "if-then-else",
            "maximum",
            "maxProperties",
            "minimum",
            "minProperties",
            "multipleOf",
            "not",
            "oneOf",
            "pattern",
            "patternProperties",
            "propertyNames",
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

            //dependencies
            "dependencies with boolean subschemas",
            "multiple dependencies subschema",
            "dependencies with escaped characters",

             // ref
            "relative pointer ref to object",
            "relative pointer ref to array",
            "escaped pointer ref",
            "remote ref, containing refs itself",
            "$ref to boolean schema true",
            "$ref to boolean schema false",
            "Recursive references between schemas",
            "refs with quote",

            // additional properties
            "non-ASCII pattern with additionalProperties", // has patternProperties
        };

        public static TheoryData<string, string, string> GetJsonSchemaTestSuite()
        {
            var result = new TheoryData<string, string, string>();
            foreach (var file in Directory.GetFiles("data/jschema/draft7", "*.json", SearchOption.AllDirectories))
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
            var schema = JsonUtility.Deserialize<JsonSchema>(schemaText, "");
            var test = JObject.Parse(testText);
            var errors = new JsonSchemaValidator(schema).Validate(test["data"]);

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
            "['warning','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,8]")]
        [InlineData("{'type': 'object'}", "1",
            "['warning','unexpected-type','Expect type 'Object' but got 'Integer'','file',1,1]")]
        [InlineData("{'type': 'string'}", "1",
            "['warning','unexpected-type','Expect type 'String' but got 'Integer'','file',1,1]")]

        // union type validation
        [InlineData("{'type': ['string', 'null']}", "'a'", "")]
        [InlineData("{'properties': {'a': {'type': ['string', 'null']}}}", "{'a': null}", "")]
        [InlineData("{'type': ['string', 'null']}", "1",
            "['warning','unexpected-type','Expect type 'String, Null' but got 'Integer'','file',1,1]")]

        // enum validation
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'a'", "")]
        [InlineData("{'type': 'string', 'enum': []}", "'unknown'",
            "['warning','undefined-value','Value 'unknown' is not accepted. Valid values: ','file',1,9]")]
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'unknown'",
            "['warning','undefined-value','Value 'unknown' is not accepted. Valid values: 'a', 'b'','file',1,9]")]

        [InlineData("{'type': 'number', 'enum': [1, 2]}", "1", "")]
        [InlineData("{'type': 'number', 'enum': [1, 2]}", "3",
            "['warning','undefined-value','Value '3' is not accepted. Valid values: '1', '2'','file',1,1]")]

        // string length validation
        [InlineData("{'type': 'string', 'minLength': 1, 'maxLength': 5}", "'a'", "")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'1963-06-19T08:30:06Z'",
            "['warning','string-length-invalid','String length should be <= 1','file',1,22]")]
        [InlineData("{'properties': {'str': {'minLength': 1, 'maxLength': 5}}}", "{'str': null}","")]
        [InlineData("{'type': 'string', 'minLength': 1}", "''",
            "['warning','string-length-invalid','String length should be >= 1','file',1,2]")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'ab'",
            "['warning','string-length-invalid','String length should be <= 1','file',1,4]")]
        [InlineData("{'properties': {'str': {'maxLength': 2, 'minLength': 4}}}", "{'str': 'abc'}",
            @"['warning','string-length-invalid','String 'str' length should be <= 2','file',1,13]
              ['warning','string-length-invalid','String 'str' length should be >= 4','file',1,13]")]

        // string format validation
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'1963-06-19T08:30:06Z'", "")]
        [InlineData("{'type': ['string', 'number'], 'format': 'date-time'}", "1", "")]
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'invalid'",
            "['warning','format-invalid','String 'invalid' is not a valid 'DateTime'','file',1,9]")]

        // properties validation
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 'value'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 1}",
            "['warning','unexpected-type','Expect type 'String' but got 'Integer'','file',1,9]")]

        // additional properties validation
        // AdditionalProperty is enabled with explicit false
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {}}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': null}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': false}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','unknown-field','Could not find member 'key1' on object of type 'String'.','file',1,33]")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'number'}}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','unexpected-type','Expect type 'Number' but got 'String'','file',1,33]")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'string', 'enum': ['a']}}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','undefined-value','Value 'value1' is not accepted. Valid values: 'a'','file',1,33]")]

        // array validation
        [InlineData("{'items': {'type': 'string'}}", "['a','b']", "")]
        [InlineData("{'items': {'type': 'boolean'}}", "['a','b']",
            @"['warning','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,4]
              ['warning','unexpected-type','Expect type 'Boolean' but got 'String'','file',1,8]")]

        [InlineData("{'maxItems': 3, 'minItems': 1}", "['a','b']", "")]
        [InlineData("{'properties': {'arr': {'maxItems': 3, 'minItems': 1}}}", "{'arr': ['a','b','c','d']}",
            "['warning','array-length-invalid','Array 'arr' length should be <= 3','file',1,9]")]
        [InlineData("{'maxItems': 3, 'minItems': 1}", "[]",
            "['warning','array-length-invalid','Array length should be >= 1','file',1,1]")]
        [InlineData("{'maxItems': 2, 'minItems': 4}", "['a','b','c']",
            @"['warning','array-length-invalid','Array length should be <= 2','file',1,1]
              ['warning','array-length-invalid','Array length should be >= 4','file',1,1]")]

        // required validation
        [InlineData("{'required': []}", "{}", "")]
        [InlineData("{'required': ['a']}", "{'a': 1}", "")]
        [InlineData("{'required': ['a']}", "{'b': 1}",
            "['warning','field-required','Missing required field 'a'','file',1,1]")]

        // dependencies validation
        [InlineData("{'dependencies': {}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1, 'key2' : 2}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1}",
            "['warning','lack-dependency','Missing field: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,1]")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1}}",
            "['warning','lack-dependency','Missing field: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,11]")]

        // either validation
        [InlineData("{'either': []}", "{}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{}",
            "['warning','either-logic-failed','At least one of these fields: 'key1', 'key2' exists','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2', 'key3']]}", "{}",
            "['warning','either-logic-failed','At least one of these fields: 'key1', 'key2', 'key3' exists','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2'], ['key3', 'key4']]}", "{}",
            "['warning','either-logic-failed','At least one of these fields: 'key1', 'key2' exists','file',1,1]\n['warning','either-logic-failed','At least one of these fields: 'key3', 'key4' exists','file',1,1]")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {}}",
            "['warning','either-logic-failed','At least one of these fields: 'key1', 'key2' exists','file',1,11]")]

        // precludes validation
        [InlineData("{'precludes': []}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}",
            "['warning','precludes-logic-failed','Only one of these fields: 'key1', 'key2' can exist at most','file',1,1]")]
        [InlineData("{'precludes': [['key1', 'key2', 'key3']]}", "{'key1': 1, 'key2': 2, 'key3': 3}",
            "['warning','precludes-logic-failed','Only one of these fields: 'key1', 'key2', 'key3' can exist at most','file',1,1]")]
        [InlineData("{'precludes': [['key1', 'key2'], ['key3', 'key4']]}", "{'key1': 1, 'key2': 2, 'key3': 3, 'key4': 4}",
            "['warning','precludes-logic-failed','Only one of these fields: 'key1', 'key2' can exist at most','file',1,1]\n['warning','precludes-logic-failed','Only one of these fields: 'key3', 'key4' can exist at most','file',1,1]")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
            "['warning','precludes-logic-failed','Only one of these fields: 'key1', 'key2' can exist at most','file',1,11]")]

        // date format validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': 'Dec 5 2018'}",
            "['warning','date-format-invalid','The 'key1' needs to meet the 'M/d/yyyy' format','file',1,21]")]

        // date range validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-10000000:00:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}",
            "['warning','over-date-range','Based on the current time, 'key1' needs to be in this range: -02:00:00 <= 'key1' <= 5.00:00:00','file',1,21]")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/4019'}",
            "['warning','over-date-range','Based on the current time, 'key1' needs to be in this range: -02:00:00 <= 'key1' <= 5.00:00:00','file',1,21]")]

        // deprecated validation
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': ''}}}", "{'key1': 1}",
            "['warning','field-deprecated','Deprecated field: 'key1'.','file',1,10]")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': 1}",
            "['warning','field-deprecated','Deprecated field: 'key1', use 'key2' instead','file',1,10]")]
        public void TestJsonSchemaValidation(string schema, string json, string expectedErrors)
        {
            var jsonSchema = JsonUtility.Deserialize<JsonSchema>(schema.Replace('\'', '"'), null);
            var (_, payload) = JsonUtility.Parse(json.Replace('\'', '"'), "file");
            var errors = new JsonSchemaValidator(jsonSchema).Validate(payload);
            var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
            var actual = string.Join('\n', errors.Select(err => err.ToString()).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }
    }
}
