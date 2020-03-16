// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        public void TestJsonSchemaConfirmance(string description, string schemaText, string testText)
        {
            var schema = JsonUtility.Deserialize<JsonSchema>(schemaText, new FilePath(""));
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
            "['warning','unexpected-type','Expected type 'Boolean' but got 'String'','file',1,8]")]
        [InlineData("{'type': 'object'}", "1",
            "['warning','unexpected-type','Expected type 'Object' but got 'Integer'','file',1,1]")]
        [InlineData("{'type': 'string'}", "1",
            "['warning','unexpected-type','Expected type 'String' but got 'Integer'','file',1,1]")]

        // union type validation
        [InlineData("{'type': ['string', 'null']}", "'a'", "")]
        [InlineData("{'properties': {'a': {'type': ['string', 'null']}}}", "{'a': null}", "")]
        [InlineData("{'type': ['string', 'null']}", "1",
            "['warning','unexpected-type','Expected type 'String, Null' but got 'Integer'','file',1,1]")]

        // const validation
        [InlineData("{'const': 1}", "1", "")]
        [InlineData("{'const': 'string'}", "'unknown'",
            "['warning','invalid-value','Invalid value for '': 'unknown'','file',1,9]")]
        [InlineData("{'const': {'a': 1}}", "{}",
            "['warning','invalid-value','Invalid value for '': '{}'','file',1,1]")]

        // enum validation
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'a'", "")]
        [InlineData("{'type': 'string', 'enum': []}", "'unknown'",
            "['warning','invalid-value','Invalid value for '': 'unknown'','file',1,9]")]
        [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'unknown'",
            "['warning','invalid-value','Invalid value for '': 'unknown'','file',1,9]")]

        [InlineData("{'type': 'number', 'enum': [1, 2]}", "1", "")]
        [InlineData("{'type': 'number', 'enum': [1, 2]}", "3",
            "['warning','invalid-value','Invalid value for '': '3'','file',1,1]")]

        // pattern validation
        [InlineData("{'pattern': '^a.*'}", "'a'", "")]
        [InlineData("{'pattern': '^a.*'}", "'b'",
            "['warning','format-invalid','String 'b' is not a valid '^a.*'','file',1,3]")]

        // string length validation
        [InlineData("{'type': 'string', 'minLength': 1, 'maxLength': 5}", "'a'", "")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'1963-06-19T08:30:06Z'",
            "['warning','string-length-invalid','String '' length should be <= 1','file',1,22]")]
        [InlineData("{'properties': {'str': {'minLength': 1, 'maxLength': 5}}}", "{'str': null}", "")]
        [InlineData("{'type': 'string', 'minLength': 1}", "''",
            "['warning','string-length-invalid','String '' length should be >= 1','file',1,2]")]
        [InlineData("{'type': 'string', 'maxLength': 1}", "'ab'",
            "['warning','string-length-invalid','String '' length should be <= 1','file',1,4]")]
        [InlineData("{'properties': {'str': {'maxLength': 2, 'minLength': 4}}}", "{'str': 'abc'}",
            @"['warning','string-length-invalid','String 'str' length should be <= 2','file',1,13]
              ['warning','string-length-invalid','String 'str' length should be >= 4','file',1,13]")]

        // number validation
        [InlineData("{'minimum': 1, 'maximum': 1}", "1", "")]
        [InlineData("{'exclusiveMinimum': 0.99, 'exclusiveMaximum': 1.01}", "1", "")]
        [InlineData("{'minimum': 100, 'maximum': -100}", "1",
            @"['warning','number-invalid','Number '1' should be <= -100','file',1,1]
              ['warning','number-invalid','Number '1' should be >= 100','file',1,1]")]
        [InlineData("{'exclusiveMinimum': 100, 'exclusiveMaximum': -100}", "1",
            @"['warning','number-invalid','Number '1' should be < -100','file',1,1]
              ['warning','number-invalid','Number '1' should be > 100','file',1,1]")]

        [InlineData("{'multipleOf': 1}", "1", "")]
        [InlineData("{'multipleOf': 0}", "1", "")]
        [InlineData("{'multipleOf': 0.0}", "1", "")]
        [InlineData("{'multipleOf': 2}", "1",
            "['warning','number-invalid','Number '1' should be multiple of 2','file',1,1]")]

        // string format validation
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'1963-06-19T08:30:06Z'", "")]
        [InlineData("{'type': ['string', 'number'], 'format': 'date-time'}", "1", "")]
        [InlineData("{'type': ['string'], 'format': 'date-time'}", "'invalid'",
            "['warning','format-invalid','String 'invalid' is not a valid 'DateTime'','file',1,9]")]

        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19'", "")]
        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-13-99'",
            "['warning','format-invalid','String '1963-13-99' is not a valid 'Date'','file',1,12]")]
        [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19T08:30:06Z'",
            "['warning','format-invalid','String '1963-06-19T08:30:06Z' is not a valid 'Date'','file',1,22]")]

        [InlineData("{'type': ['string'], 'format': 'time'}", "'08:30:06Z'", "")]
        [InlineData("{'type': ['string'], 'format': 'time'}", "'1963-06-19T08:30:06Z'",
            "['warning','format-invalid','String '1963-06-19T08:30:06Z' is not a valid 'Time'','file',1,22]")]

        // properties validation
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 'value'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 1}",
            "['warning','unexpected-type','Expected type 'String' but got 'Integer'','file',1,9]")]

        // additional properties validation
        // AdditionalProperty is enabled with explicit false
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {}}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': null}", "{'key': 'value', 'key1': 'value1'}", "")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': false}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','unknown-field','Could not find member 'key1' on object of type 'String'.','file',1,33]")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'number'}}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','unexpected-type','Expected type 'Number' but got 'String'','file',1,33]")]
        [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'string', 'enum': ['a']}}", "{'key': 'value', 'key1': 'value1'}",
            "['warning','invalid-value','Invalid value for '': 'value1'','file',1,33]")]

        // property name validation
        [InlineData("{'propertyNames': {'maxLength': 1}}", "{'a': 0}", "")]
        [InlineData("{'propertyNames': {'maxLength': 1}}", "{'ab': 0}",
            "['warning','string-length-invalid','String 'ab' length should be <= 1','file',1,6]")]

        // property count validation
        [InlineData("{'maxProperties': 3}", "{}", "")]
        [InlineData("{'maxProperties': 0}", "{'key': 0}",
            "['warning','property-count-invalid','Object '' property count should be <= 0','file',1,1]")]
        [InlineData("{'minProperties': 1}", "{}",
            "['warning','property-count-invalid','Object '' property count should be >= 1','file',1,1]")]
        [InlineData("{'maxProperties': 0, 'minProperties': 4}", "{'key': 0}",
            @"['warning','property-count-invalid','Object '' property count should be <= 0','file',1,1]
              ['warning','property-count-invalid','Object '' property count should be >= 4','file',1,1]")]

        // array validation
        [InlineData("{'items': {'type': 'string'}}", "['a','b']", "")]
        [InlineData("{'items': {'type': 'boolean'}}", "['a','b']",
            @"['warning','unexpected-type','Expected type 'Boolean' but got 'String'','file',1,4]
              ['warning','unexpected-type','Expected type 'Boolean' but got 'String'','file',1,8]")]

        [InlineData("{'maxItems': 3, 'minItems': 1}", "['a','b']", "")]
        [InlineData("{'properties': {'arr': {'maxItems': 3, 'minItems': 1}}}", "{'arr': ['a','b','c','d']}",
            "['warning','array-length-invalid','Array 'arr' length should be <= 3','file',1,9]")]
        [InlineData("{'maxItems': 3, 'minItems': 1}", "[]",
            "['warning','array-length-invalid','Array '' length should be >= 1','file',1,1]")]
        [InlineData("{'maxItems': 2, 'minItems': 4}", "['a','b','c']",
            @"['warning','array-length-invalid','Array '' length should be <= 2','file',1,1]
              ['warning','array-length-invalid','Array '' length should be >= 4','file',1,1]")]

        // uniqueItems validation
        [InlineData("{'uniqueItems': true}", "[1, 2]", "")]
        [InlineData("{'uniqueItems': true}", "[1, 1]",
            @"['warning','array-not-unique','Array '' items should be unique','file',1,1]")]

        // contains validation
        [InlineData("{'contains': {'const': 1}}", "[1]", "")]
        [InlineData("{'contains': {'const': 1}}", "[2]",
            @"['warning','array-contains-failed','Array '' should contain at least one item that matches JSON schema','file',1,1]")]

        // additionalItems validation
        [InlineData("{'items': [{'const': 1}], 'additionalItems': true}", "[1]", "")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1]", "")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1, 2]",
            "['warning','array-length-invalid','Array '' length should be <= 1','file',1,1]")]
        [InlineData("{'items': [{'const': 1}], 'additionalItems': {'const': 2}}", "[1, 3]",
            "['warning','invalid-value','Invalid value for '': '3'','file',1,5]")]

        // required validation
        [InlineData("{'required': []}", "{}", "")]
        [InlineData("{'required': ['a']}", "{'a': 1}", "")]
        [InlineData("{'required': ['a']}", "{'b': 1}",
            "['warning','missing-attribute','Missing required attribute: 'a'','file',1,1]")]

        // boolean schema
        [InlineData("true", "[]", "")]
        [InlineData("false", "[]",
            "['warning','boolean-schema-failed','Boolean schema validation failed for ''','file',1,1]")]

        // dependencies validation
        [InlineData("{'dependencies': {}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1, 'key2' : 2}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1}",
            "['warning','missing-paired-attribute','Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,1]")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : '1', 'key2': null}",
            "['warning','missing-paired-attribute','Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,1]")]
        [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : '1', 'key2': ''}",
            "['warning','missing-paired-attribute','Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,1]")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'dependencies': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1}}",
            "['warning','missing-paired-attribute','Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'','file',1,11]")]

        // dependencies as schema
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{}", "")]
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a', 'key2': 'b'}", "")]
        [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a'}",
            "['warning','missing-attribute','Missing required attribute: 'key2'','file',1,1]")]

        // either validation
        [InlineData("{'either': []}", "{}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}", "")]
        [InlineData("{'either': [['key1', 'key2']]}", "{}",
            "['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2'','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': null}",
            "['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2'','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2']]}", "{'key1': ''}",
            "['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2'','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2', 'key3']]}", "{}",
            "['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2', 'key3'','file',1,1]")]
        [InlineData("{'either': [['key1', 'key2'], ['key3', 'key4']]}", "{}",
            @"['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2'','file',1,1]
              ['warning','missing-either-attribute','One of the following attributes is required: 'key3', 'key4'','file',1,1]")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}", "")]
        [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {}}",
            "['warning','missing-either-attribute','One of the following attributes is required: 'key1', 'key2'','file',1,11]")]

        // precludes validation
        [InlineData("{'precludes': []}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': null}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': ''}", "")]
        [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}",
            "['warning','precluded-attributes','Only one of the following attributes can exist: 'key1', 'key2'','file',1,1]")]
        [InlineData("{'precludes': [['key1', 'key2', 'key3']]}", "{'key1': 1, 'key2': 2, 'key3': 3}",
            "['warning','precluded-attributes','Only one of the following attributes can exist: 'key1', 'key2', 'key3'','file',1,1]")]
        [InlineData("{'precludes': [['key1', 'key2'], ['key3', 'key4']]}", "{'key1': 1, 'key2': 2, 'key3': 3, 'key4': 4}",
            @"['warning','precluded-attributes','Only one of the following attributes can exist: 'key1', 'key2'','file',1,1]
              ['warning','precluded-attributes','Only one of the following attributes can exist: 'key3', 'key4'','file',1,1]")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
            "['warning','precluded-attributes','Only one of the following attributes can exist: 'key1', 'key2'','file',1,11]")]

        // date format validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': null}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': ''}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': 'Dec 5 2018'}",
            "['warning','date-format-invalid','Invalid date format for 'key1': 'Dec 5 2018'.','file',1,21]")]

        // date range validation
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-10000000:00:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}", "")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}",
            "['warning','date-out-of-range','Value out of range for 'key1': '04/26/2019'','file',1,21]")]
        [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/4019'}",
            "['warning','date-out-of-range','Value out of range for 'key1': '04/26/4019'','file',1,21]")]

        // deprecated validation
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': null}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': ''}", "")]
        [InlineData("{'properties': {'key1': {'replacedBy': ''}}}", "{'key1': 1}",
            "['warning','attribute-deprecated','Deprecated attribute: 'key1'.','file',1,10]")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': 1}",
            "['warning','attribute-deprecated','Deprecated attribute: 'key1', use 'key2' instead','file',1,10]")]

        // enum dependencies validation
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': ['null', 'string']}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': null}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'tabs'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': null, 'yammer': null}}}", "{'key1': 'yammer'}", "")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yyy', 'key2': 'tabs'}",
            "['warning','invalid-value','Invalid value for 'key1': 'yyy'','file',1,14]")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yyy', 'key2': 'tabs'}",
            "['warning','invalid-value','Invalid value for 'key1': 'yyy'','file',1,14]")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
            "['warning','invalid-paired-attribute','Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'','file',1,32]")]
        [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
            "['warning','invalid-paired-attribute','Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'','file',1,32]")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['yammer','abc']}",
            "['warning','invalid-paired-attribute','Invalid value for 'key1[1]': 'abc' is not valid with 'key1[0]' value 'yammer'','file',1,24]")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','vst']}", "")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','abc']}",
            "['warning','invalid-paired-attribute','Invalid value for 'key1[2]': 'abc' is not valid with 'key1[1]' value 'tabs'','file',1,31]")]
        [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['yyy','tabs']}",
            "['warning','invalid-value','Invalid value for 'key1[0]': 'yyy'','file',1,15]")]

        // custom errors
        [InlineData("{'required': ['author'], 'customErrors': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
            "['suggestion','author-missing','Missing required attribute: 'author'. Add a valid GitHub ID.','file',1,1]")]
        [InlineData("{'required': ['author'], 'customErrors': {'author': {'missing-attribute': {'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
            "['warning','author-missing','Missing required attribute: 'author'. Add a valid GitHub ID.','file',1,1]")]
        [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}, 'customErrors': {'key1': {'attribute-deprecated': {'severity': 'suggestion', 'code': 'key1-attribute-deprecated'}}}}", "{'key1': 1}",
            "['suggestion','key1-attribute-deprecated','Deprecated attribute: 'key1', use 'key2' instead','file',1,10]")]
        [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}, 'customErrors': {'key1': {'precluded-attributes': {'severity': 'error'}}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
            "['error','precluded-attributes','Only one of the following attributes can exist: 'key1', 'key2'','file',1,11]")]

        // strict required validation
        [InlineData("{'strictRequired': ['key1']}", "{'key1': 'a'}", "")]
        [InlineData("{'strictRequired': ['key1']}", "{}",
            "['warning','missing-attribute','Missing required attribute: 'key1'','file',1,1]")]
        [InlineData("{'strictRequired': ['key1']}", "{'key1': null}",
            "['warning','missing-attribute','Missing required attribute: 'key1'','file',1,1]")]
        [InlineData("{'strictRequired': ['key1']}", "{'key1': ''}",
            "['warning','missing-attribute','Missing required attribute: 'key1'','file',1,1]")]
        public void TestJsonSchemaValidation(string schema, string json, string expectedErrors)
        {
            var jsonSchema = JsonUtility.Deserialize<JsonSchema>(schema.Replace('\'', '"'), null);
            var (_, payload) = JsonUtility.Parse(json.Replace('\'', '"'), new FilePath("file"));
            var errors = new JsonSchemaValidator(jsonSchema).Validate(payload);
            var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
            var actual = string.Join('\n', errors.Select(err => err.ToString().Replace("\\r", "")).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }


        [Theory]
        // attribut docset unique validation
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}" }, "")]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}" , "{'key1': 'b'}" }, "")]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key1': 'a'}" },
            "['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file1',1,12]\n['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file2',1,12]")]
        [InlineData("{'docsetUnique': ['key1']}", new[] { "{'key1': 'a'}", "{'key2': 'a', 'key1': 'a'}" },
            "['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file1',1,12]\n['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file2',1,25]")]
        [InlineData("{'docsetUnique': ['key1', 'key2']}", new[] { "{'key1': 'a'}", "{'key2': 'a', 'key1': 'a'}" },
            "['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file1',1,12]\n['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file2',1,25]")]
        [InlineData("{'docsetUnique': ['key1', 'key2']}", new[] { "{'key1': 'a', 'key2': 'b'}", "{'key2': 'b', 'key1': 'a'}" },
            "['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file1',1,12]\n['suggestion','duplicate-attribute','Attribute 'key1' with value 'a' is duplicated in 'file1', 'file2'','file2',1,25]\n" +
            "['suggestion','duplicate-attribute','Attribute 'key2' with value 'b' is duplicated in 'file1', 'file2'','file1',1,25]\n['suggestion','duplicate-attribute','Attribute 'key2' with value 'b' is duplicated in 'file1', 'file2'','file2',1,12]")]
        [InlineData("{'docsetUnique': ['key11']}", new[] { "{'key1': {'key11': 'a'}}", "'key11': 'a'}" }, "")]
        [InlineData("{'properties': {'key1': {'docsetUnique': ['key11']}}}", new[] { "{'key1': {'key11': 'a'}}", "{'key1': {'key11': 'a'}, 'key11': 'a'}" },
            "['suggestion','duplicate-attribute','Attribute 'key11' with value 'a' is duplicated in 'file1', 'file2'','file1',1,22]\n['suggestion','duplicate-attribute','Attribute 'key11' with value 'a' is duplicated in 'file1', 'file2'','file2',1,22]")]
        public void TestJsonSchemaPostValidation(string schema, string [] jsons, string expectedErrors)
        {
            var jsonSchema = JsonUtility.Deserialize<JsonSchema>(schema.Replace('\'', '"'), null);
            var payloads = Enumerable.Range(0, jsons.Length).Select(i => JsonUtility.Parse(jsons[i].Replace('\'', '"'), new FilePath($"file{i+1}")).value);
            var jsonSchemaValidator = new JsonSchemaValidator(jsonSchema, null);

            foreach(var payload in payloads)
            {
                jsonSchemaValidator.Validate(payload);
            }

            var errors = jsonSchemaValidator.PostValidate();
            var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
            var actual = string.Join('\n', errors.Select(err => err.ToString().Replace("\\r", "")).OrderBy(err => err).ToArray()).Replace('"', '\'');
            Assert.Equal(expected, actual);
        }
    }
}
