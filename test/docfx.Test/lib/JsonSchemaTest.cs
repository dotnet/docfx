// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            "allOf",
            "anyOf",
            "definitions",
            "if-then-else",
            "not",
            "oneOf",
            "refRemote"
        };

        private static readonly string[] s_notSupportedTests =
        {
             // ref
            "relative pointer ref to object",
            "relative pointer ref to array",
            "escaped pointer ref",
            "remote ref, containing refs itself",
            "Recursive references between schemas",
            "refs with quote",
            "Location-independent identifier",
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
                    var schemaText = schema["schema"].ToString(Formatting.None);
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
        public void TestJsonSchemaConformance(string description, string schemaText, string testText)
        {
            var schema = JsonUtility.DeserializeData<JsonSchema>(schemaText, new FilePath(""));
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
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','file':'file','line':1,'end_line':1,'column':8,'end_column':8,'hidden':false}")]
        [InlineData("{'type': 'object'}", "1",
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'Object' but got 'Integer'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'type': 'string'}", "1",
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'String' but got 'Integer'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // union type validation
        [InlineData("{'type': ['string', 'null']}", "'a'", "")]
        [InlineData("{'properties': {'a': {'type': ['string', 'null']}}}", "{'a': null}", "")]
        [InlineData("{'type': ['string', 'null']}", "1",
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'String, Null' but got 'Integer'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // const validation
        [InlineData("{'const': 1}", "1", "")]
        [InlineData("{'const': 'string'}", "'unknown'",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': 'unknown'.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]
        [InlineData("{'const': {'a': 1}}", "{}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': '{}'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // enum validation
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'a'", "")]
        [InlineData("{'type': 'string', 'enum': []}", "'unknown'",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': 'unknown'.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'unknown'",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': 'unknown'.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]

        [InlineData("{'type': 'number', 'enum': [1, 2]}", "1", "")]
        [InlineData("{'type': 'number', 'enum': [1, 2]}", "3",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': '3'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // pattern validation
        [InlineData("{'pattern': '^a.*'}", "'a'", "")]
        [InlineData("{'pattern': '^a.*'}", "'b'",
            "{'message_severity':'warning','log_item_type':'user','code':'format-invalid','message':'String 'b' is not a valid '^a.*'.','file':'file','line':1,'end_line':1,'column':3,'end_column':3,'hidden':false}")]

        // string length validation
        [InlineData("{'type': 'string', 'minLength': 1, 'maxLength': 5}", "'a'", "")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'1963-06-19T08:30:06Z'",
            "{'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String '' is too long: 20 characters. Length should be <= 1.','file':'file','line':1,'end_line':1,'column':22,'end_column':22,'hidden':false}")]
        [InlineData("{'properties': {'str': {'minLength': 1, 'maxLength': 5}}}", "{'str': null}", "")]
        [InlineData("{'type': 'string', 'minLength': 1}", "''",
            "{'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String '' is too short: 0 characters. Length should be >= 1.','file':'file','line':1,'end_line':1,'column':2,'end_column':2,'hidden':false}")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'ab'",
            "{'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String '' is too long: 2 characters. Length should be <= 1.','file':'file','line':1,'end_line':1,'column':4,'end_column':4,'hidden':false}")]
        [InlineData("{'properties': {'str': {'maxLength': 2, 'minLength': 4}}}", "{'str': 'abc'}",
            @"{'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String 'str' is too long: 3 characters. Length should be <= 2.','file':'file','line':1,'end_line':1,'column':13,'end_column':13,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String 'str' is too short: 3 characters. Length should be >= 4.','file':'file','line':1,'end_line':1,'column':13,'end_column':13,'hidden':false}")]

        // number validation
        [InlineData("{'minimum': 1, 'maximum': 1}", "1", "")]
        [InlineData("{'exclusiveMinimum': 0.99, 'exclusiveMaximum': 1.01}", "1", "")]
        [InlineData("{'minimum': 100, 'maximum': -100}", "1",
            @"{'message_severity':'warning','log_item_type':'user','code':'number-invalid','message':'Number '1' should be <= -100.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'number-invalid','message':'Number '1' should be >= 100.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'exclusiveMinimum': 100, 'exclusiveMaximum': -100}", "1",
            @"{'message_severity':'warning','log_item_type':'user','code':'number-invalid','message':'Number '1' should be < -100.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'number-invalid','message':'Number '1' should be > 100.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        [InlineData("{'multipleOf': 1}", "1", "")]
        [InlineData("{'multipleOf': 0}", "1", "")]
        [InlineData("{'multipleOf': 0.0}", "1", "")]
        [InlineData("{'multipleOf': 2}", "1",
            "{'message_severity':'warning','log_item_type':'user','code':'number-invalid','message':'Number '1' should be multiple of 2.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // string format validation
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'1963-06-19T08:30:06Z'", "")]
        [InlineData("{'type': ['string', 'number'], 'format': 'date-time'}", "1", "")]
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'invalid'",
            "{'message_severity':'warning','log_item_type':'user','code':'format-invalid','message':'String 'invalid' is not a valid 'DateTime'.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]

        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19'", "")]
        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-13-99'",
            "{'message_severity':'warning','log_item_type':'user','code':'format-invalid','message':'String '1963-13-99' is not a valid 'Date'.','file':'file','line':1,'end_line':1,'column':12,'end_column':12,'hidden':false}")]
        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19T08:30:06Z'",
            "{'message_severity':'warning','log_item_type':'user','code':'format-invalid','message':'String '1963-06-19T08:30:06Z' is not a valid 'Date'.','file':'file','line':1,'end_line':1,'column':22,'end_column':22,'hidden':false}")]

        [InlineData("{'type': ['string'], 'format': 'time'}", "'08:30:06Z'", "")]
        [InlineData("{'type': ['string'], 'format': 'time'}", "'1963-06-19T08:30:06Z'",
            "{'message_severity':'warning','log_item_type':'user','code':'format-invalid','message':'String '1963-06-19T08:30:06Z' is not a valid 'Time'.','file':'file','line':1,'end_line':1,'column':22,'end_column':22,'hidden':false}")]

        // properties validation
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 'value'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'String' but got 'Integer'.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]

        // additional properties validation
        // AdditionalProperty is enabled with explicit false
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {}}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': null}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': false}", "{'key': 'value', 'key1': 'value1'}",
            "{'message_severity':'warning','log_item_type':'user','code':'unknown-field','message':'Could not find member 'key1' on object of type 'String'.','file':'file','line':1,'end_line':1,'column':33,'end_column':33,'hidden':false}")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'number'}}", "{'key': 'value', 'key1': 'value1'}",
            "{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'Number' but got 'String'.','file':'file','line':1,'end_line':1,'column':33,'end_column':33,'hidden':false}")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'string', 'enum': ['a']}}", "{'key': 'value', 'key1': 'value1'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': 'value1'.','file':'file','line':1,'end_line':1,'column':33,'end_column':33,'hidden':false}")]

        // property name validation
        [InlineData("{'propertyNames': {'maxLength': 1}}", "{'a': 0}", "")]
        [InlineData("{'propertyNames': {'maxLength': 1}}", "{'ab': 0}",
            "{'message_severity':'warning','log_item_type':'user','code':'string-length-invalid','message':'String 'ab' is too long: 2 characters. Length should be <= 1.','file':'file','line':1,'end_line':1,'column':6,'end_column':6,'hidden':false}")]

        // property count validation
        [InlineData("{'maxProperties': 3}", "{}", "")]
        [InlineData("{'maxProperties': 0}", "{'key': 0}",
            "{'message_severity':'warning','log_item_type':'user','code':'property-count-invalid','message':'Object '' property count should be <= 0.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'minProperties': 1}", "{}",
            "{'message_severity':'warning','log_item_type':'user','code':'property-count-invalid','message':'Object '' property count should be >= 1.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'maxProperties': 0, 'minProperties': 4}", "{'key': 0}",
            @"{'message_severity':'warning','log_item_type':'user','code':'property-count-invalid','message':'Object '' property count should be <= 0.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'property-count-invalid','message':'Object '' property count should be >= 4.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // array validation
        [InlineData("{'items': {'type': 'string'}}", "['a','b']", "")]
        [InlineData("{'items': {'type': 'boolean'}}", "['a','b']",
            @"{'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','file':'file','line':1,'end_line':1,'column':4,'end_column':4,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','file':'file','line':1,'end_line':1,'column':8,'end_column':8,'hidden':false}")]

        [InlineData("{'maxItems': 3, 'minItems': 1}", "['a','b']", "")]
        [InlineData("{'properties': {'arr': {'maxItems': 3, 'minItems': 1}}}", "{'arr': ['a','b','c','d']}",
            "{'message_severity':'warning','log_item_type':'user','code':'array-length-invalid','message':'Array 'arr' length should be <= 3.','file':'file','line':1,'end_line':1,'column':9,'end_column':9,'hidden':false}")]
        [InlineData("{'maxItems': 3, 'minItems': 1}", "[]",
            "{'message_severity':'warning','log_item_type':'user','code':'array-length-invalid','message':'Array '' length should be >= 1.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'maxItems': 2, 'minItems': 4}", "['a','b','c']",
            @"{'message_severity':'warning','log_item_type':'user','code':'array-length-invalid','message':'Array '' length should be <= 2.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'array-length-invalid','message':'Array '' length should be >= 4.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // uniqueItems validation
        [InlineData("{'uniqueItems': true}", "[1, 2]", "")]
        [InlineData("{'uniqueItems': true}", "[1, 1]",
            @"{'message_severity':'warning','log_item_type':'user','code':'array-not-unique','message':'Array '' items should be unique.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // contains validation
        [InlineData("{'contains': {'const': 1}}", "[1]", "")]
        [InlineData("{'contains': {'const': 1}}", "[2]",
            @"{'message_severity':'warning','log_item_type':'user','code':'array-contains-failed','message':'Array '' should contain at least one item that matches JSON schema.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // additionalItems validation
        [InlineData("{'items': [{'const': 1}], 'additionalItems': true}", "[1]", "")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1]", "")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1, 2]",
            "{'message_severity':'warning','log_item_type':'user','code':'array-length-invalid','message':'Array '' length should be <= 1.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': {'const': 2}}", "[1, 3]",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for '': '3'.','file':'file','line':1,'end_line':1,'column':5,'end_column':5,'hidden':false}")]

        // required validation
        [InlineData("{'required': []}", "{}", "")]
        [InlineData("{'required': ['a']}", "{'a': 1}", "")]
        [InlineData("{'required': ['a']}", "{'b': 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-attribute','message':'Missing required attribute: 'a'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // boolean schema
        [InlineData("true", "[]", "")]
        [InlineData("false", "[]",
            "{'message_severity':'warning','log_item_type':'user','code':'boolean-schema-failed','message':'Boolean schema validation failed for ''.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // dependencies validation
        [InlineData("{'dependencies': {}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1, 'key2' : 2}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : '1', 'key2': null}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : '1', 'key2': ''}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1}}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','file':'file','line':1,'end_line':1,'column':11,'end_column':11,'hidden':false}")]

        // dependencies as schema
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a', 'key2': 'b'}", "")]
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a'}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-attribute','message':'Missing required attribute: 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]

        // either validation
        [InlineData("{'either': []}", "{}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': null}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': ''}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'either': [['key1', 'key2', 'key3']]}", "{}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2', 'key3'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'either': [['key1', 'key2'], ['key3', 'key4']]}", "{}",
            @"{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key3', 'key4'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {}}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':11,'end_column':11,'hidden':false}")]

        // precludes validation
        [InlineData("{'precludes': []}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': null}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': ''}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}",
            "{'message_severity':'warning','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'precludes': [['key1', 'key2', 'key3']]}", "{'key1': 1, 'key2': 2, 'key3': 3}",
            "{'message_severity':'warning','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2', 'key3'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'precludes': [['key1', 'key2'], ['key3', 'key4']]}", "{'key1': 1, 'key2': 2, 'key3': 3, 'key4': 4}",
            @"{'message_severity':'warning','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}
              {'message_severity':'warning','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key3', 'key4'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
            "{'message_severity':'warning','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':11,'end_column':11,'hidden':false}")]

        // date format validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': null}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': ''}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': 'Dec 5 2018'}",
            "{'message_severity':'warning','log_item_type':'user','code':'date-format-invalid','message':'Invalid date format for 'key1': 'Dec 5 2018'.','file':'file','line':1,'end_line':1,'column':21,'end_column':21,'hidden':false}")]

        // date range validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-10000000:00:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}",
            "{'message_severity':'warning','log_item_type':'user','code':'date-out-of-range','message':'Value out of range for 'key1': '04/26/2019'.','file':'file','line':1,'end_line':1,'column':21,'end_column':21,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/4019'}",
            "{'message_severity':'warning','log_item_type':'user','code':'date-out-of-range','message':'Value out of range for 'key1': '04/26/4019'.','file':'file','line':1,'end_line':1,'column':21,'end_column':21,'hidden':false}")]

        // deprecated validation
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': null}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': ''}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': ''}}}", "{'key1': 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'attribute-deprecated','message':'Deprecated attribute: 'key1'.','file':'file','line':1,'end_line':1,'column':10,'end_column':10,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'attribute-deprecated','message':'Deprecated attribute: 'key1', use 'key2' instead.','file':'file','line':1,'end_line':1,'column':10,'end_column':10,'hidden':false}")]

        // enum dependencies validation
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'key2' is not valid with 'key1' value 'yammer'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'': null, 'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'': null, 'tabs': null, 'vba': null}}}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': ['null', 'string']}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': null}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'tabs'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': null, 'yammer': null}}}", "{'key1': 'yammer'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yyy', 'key2': 'tabs'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for 'key1': 'yyy'.','file':'file','line':1,'end_line':1,'column':14,'end_column':14,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yyy', 'key2': 'tabs'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for 'key1': 'yyy'.','file':'file','line':1,'end_line':1,'column':14,'end_column':14,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'.','file':'file','line':1,'end_line':1,'column':32,'end_column':32,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'.','file':'file','line':1,'end_line':1,'column':32,'end_column':32,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['yammer','abc']}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-paired-attribute','message':'Invalid value for 'key1[1]': 'abc' is not valid with 'key1[0]' value 'yammer'.','file':'file','line':1,'end_line':1,'column':24,'end_column':24,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','vst']}", "")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','abc']}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-paired-attribute','message':'Invalid value for 'key1[2]': 'abc' is not valid with 'key1[1]' value 'tabs'.','file':'file','line':1,'end_line':1,'column':31,'end_column':31,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['yyy','tabs']}",
            "{'message_severity':'warning','log_item_type':'user','code':'invalid-value','message':'Invalid value for 'key1[0]': 'yyy'.','file':'file','line':1,'end_line':1,'column':15,'end_column':15,'hidden':false}")]

        // custom errors
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
            "{'message_severity':'suggestion','log_item_type':'user','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}, 'customRules': {'key1': {'attribute-deprecated': {'severity': 'suggestion', 'code': 'key1-attribute-deprecated'}}}}", "{'key1': 1}",
            "{'message_severity':'suggestion','log_item_type':'user','code':'key1-attribute-deprecated','message':'Deprecated attribute: 'key1', use 'key2' instead.','file':'file','line':1,'end_line':1,'column':10,'end_column':10,'hidden':false}")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}, 'customRules': {'key1': {'precluded-attributes': {'severity': 'error'}}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
            "{'message_severity':'error','log_item_type':'user','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','file':'file','line':1,'end_line':1,'column':11,'end_column':11,'hidden':false}")]
        [InlineData("{'dependencies': {'key1': ['key2']}, 'customRules': {'key1': {'missing-paired-attribute': {'code': 'key2-missing'}}}}", "{'key1' : 1}",
            "{'message_severity':'warning','log_item_type':'user','code':'key2-missing','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.', 'pullRequestOnly': true}}}}", "{'b': 1}",
            "{'message_severity':'suggestion','log_item_type':'user','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':true}")]

        // strict required validation
        [InlineData("{'strictRequired': ['key1']}", "{'key1': 'a'}", "")]
        [InlineData("{'strictRequired': ['key1']}", "{}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-attribute','message':'Missing required attribute: 'key1'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'strictRequired': ['key1']}", "{'key1': null}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-attribute','message':'Missing required attribute: 'key1'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        [InlineData("{'strictRequired': ['key1']}", "{'key1': ''}",
            "{'message_severity':'warning','log_item_type':'user','code':'missing-attribute','message':'Missing required attribute: 'key1'.','file':'file','line':1,'end_line':1,'column':1,'end_column':1,'hidden':false}")]
        public void TestJsonSchemaValidation(string schema, string json, string expectedErrors)
        {
            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(schema.Replace('\'', '"'), null);
            var (_, payload) = JsonUtility.Parse(json.Replace('\'', '"'), new FilePath("file"));
            var errors = new JsonSchemaValidator(jsonSchema).Validate(payload);
            var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
            var actual = string.Join('\n', errors.Select(err => Regex.Replace(err.ToString().Replace("\\r", ""), @",""date_time"":"".*""", "")).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }


        [Theory]

        // attribute docset unique validation
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}" }, 0)]
        [InlineData("{'docsetUnique': ['key1', 'key1']}", new[] { "{'key1': 'a'}" }, 0)]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'b'}" }, 0)]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, 2)]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key2': 'a', 'key1': 'a'}" }, 2)]
        [InlineData("{'docsetUnique': ['key1', 'key2']}", new[] { "{'key1': 'a'}", "{'key2': 'a', 'key1': 'a'}" }, 2)]
        [InlineData("{'docsetUnique': ['key1', 'key2']}", new[] { "{'key1': 'a', 'key2': 'b'}", "{'key2': 'b', 'key1': 'a'}" }, 4)]
        [InlineData("{'docsetUnique': ['key11']}", new[] { "{'key1': {'key11': 'a'}}", "'key11': 'a'}" }, 0)]
        [InlineData("{'properties': {'key1': {'docsetUnique': ['key11']}}}", new[] { "{'key1': {'key11': 'a'}}", "{'key1': {'key11': 'a'}, 'key11': 'a'}" }, 2)]
        public void TestJsonSchemaPostValidation(string schema, string[] jsons, int errorCount)
        {
            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(schema.Replace('\'', '"'), null);
            var payloads = Enumerable.Range(0, jsons.Length).Select(i => JsonUtility.Parse(jsons[i].Replace('\'', '"'), new FilePath($"file{i + 1}")).value);
            var jsonSchemaValidator = new JsonSchemaValidator(jsonSchema, null);

            foreach (var payload in payloads)
            {
                jsonSchemaValidator.Validate(payload);
            }

            var errors = jsonSchemaValidator.PostValidate();
            Assert.Equal(errorCount, errors.Count);
        }

        [Theory]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'canonicalVersionOnly': true}}}}",
            new[] { "{'key1': 'a'}" }, new[] { "" }, new[] { "missing-attribute:Warning" })]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'canonicalVersionOnly': true}}}}",
            new[] { "{'key1': 'a'}" }, new[] { "t" }, new[] { "missing-attribute:Warning" })]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'canonicalVersionOnly': true}}}}",
            new[] { "{'key1': 'a'}" }, new[] { "f" }, new[] { "missing-attribute:Off" })]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'canonicalVersionOnly': true}}}}",
            new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "f" }, new[] { "missing-attribute:Warning", "missing-attribute:Off" })]
        [InlineData("{'required': ['author'], 'customRules': {'author': {'missing-attribute': {'canonicalVersionOnly': true}}}}",
            new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "t", "f" }, new[] { "missing-attribute:Warning", "missing-attribute:Off" })]
        [InlineData("{'required': ['author']}",
            new[] { "{'key1': 'a'}" }, new[] { "" }, new[] { "missing-attribute:Warning" })]
        [InlineData("{'required': ['author']}",
            new[] { "{'key1': 'a'}" }, new[] { "t" }, new[] { "missing-attribute:Warning" })]
        [InlineData("{'required': ['author']}",
            new[] { "{'key1': 'a'}" }, new[] { "f" }, new[] { "missing-attribute:Warning" })]

        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "t" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "t", "t" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "t", "f" }, new string[] { })]
        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "f", "f" }, new string[] { })]
        [InlineData("{'docsetUnique': ['key1'], 'customRules': {'key1': {'duplicate-attribute': {'canonicalVersionOnly': true}}}}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "f" }, new string[] { })]

        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "t" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "t", "t" }, new[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "t", "f" }, new string[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "f", "f" }, new string[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" }, new[] { "", "f" }, new string[] { "duplicate-attribute:Suggestion", "duplicate-attribute:Suggestion" })]
        public void TestJsonSchemaCanonicalVersionValidation(string schema, string[] jsons, string[] isCanonicalVersions, string[] errors)
        {
            Assert.Equal(jsons.Length, isCanonicalVersions.Length);
            var jsonSchema = JsonUtility.DeserializeData<JsonSchema>(schema.Replace('\'', '"'), null);
            var payloads = Enumerable.Range(0, jsons.Length).Select(i => JsonUtility.Parse(jsons[i].Replace('\'', '"'), new FilePath($"file{i + 1}")).value).ToArray();
            var jsonSchemaValidator = new JsonSchemaValidator(jsonSchema, null);

            var validationErrors = new List<Error>();
            for (var i = 0; i < payloads.Length; i++)
            {
                var isCanonicalVersion = isCanonicalVersions[i] == ""
                    ? null
                    : (bool?)(isCanonicalVersions[i] == "t" ? true : false);
                validationErrors.AddRange(jsonSchemaValidator.Validate(payloads[i], isCanonicalVersion));
            }
            validationErrors.AddRange(jsonSchemaValidator.PostValidate());
            Assert.Equal(errors.OrderBy(e => e), validationErrors.OrderBy(e => e.Code).ThenBy(e => e.Level).Select(e => $"{e.Code}:{e.Level}"));
        }
    }
}
