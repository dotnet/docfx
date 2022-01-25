// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build;

public class JsonSchemaTest
{
    private static readonly JsonSchemaLoader s_jsonSchemaLoader = new(new(new LocalPackage()), FetchRemoteSchema);

    public static TheoryData<string, string, string> GetJsonSchemaTestSuite()
    {
        var result = new TheoryData<string, string, string>();
        foreach (var file in Directory.GetFiles("data/jschema/draft7", "*.json", SearchOption.AllDirectories))
        {
            var i = 0;
            var suite = Path.GetFileNameWithoutExtension(file);
            foreach (var schema in JArray.Parse(File.ReadAllText(file)))
            {
                var schemaText = schema["schema"].ToString(Formatting.None);
                foreach (var test in schema["tests"])
                {
                    var description = $"[{++i:d2}]{schema["description"]}/{test["description"]}";
                    result.Add($"{suite}/{description}", schemaText, test.ToString());
                }
            }
        }
        return result;
    }

    [Theory]
    [MemberData(nameof(GetJsonSchemaTestSuite))]
    public void TestJsonSchemaConformance(string description, string schema, string testText)
    {
        var jsonSchema = s_jsonSchemaLoader.LoadSchema(schema);
        var test = JObject.Parse(testText);
        var errors = new JsonSchemaValidator(jsonSchema).Validate(test["data"], new("file"));

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
    [InlineData(
        "{'type': 'boolean'}",
        "'string'",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','line':1,'column':8}")]
    [InlineData(
        "{'type': 'object'}",
        "1",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'Object' but got 'Integer'.','line':1,'column':1}")]
    [InlineData(
        "{'type': 'string'}",
        "1",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'String' but got 'Integer'.','line':1,'column':1}")]

    // union type validation
    [InlineData("{'type': ['string', 'null']}", "'a'", "")]
    [InlineData("{'properties': {'a': {'type': ['string', 'null']}}}", "{'a': null}", "")]
    [InlineData(
        "{'type': ['string', 'null']}",
        "1",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'String, Null' but got 'Integer'.','line':1,'column':1}")]

    // const validation
    [InlineData("{'const': 1}", "1", "")]
    [InlineData(
        "{'const': 'string'}",
        "'unknown'",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': 'unknown'.','line':1,'column':9}")]
    [InlineData("{'const': {'a': 1}}", "{}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': '{}'.','line':1,'column':1}")]

    // enum validation
    [InlineData("{'type': 'string', 'enum': ['a', 'b']}", "'a'", "")]
    [InlineData(
        "{'type': 'string', 'enum': []}",
        "'unknown'",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': 'unknown'.','line':1,'column':9}")]
    [InlineData(
        "{'type': 'string', 'enum': ['a', 'b']}",
        "'unknown'",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': 'unknown'.','line':1,'column':9}")]

    [InlineData("{'type': 'number', 'enum': [1, 2]}", "1", "")]
    [InlineData(
        "{'type': 'number', 'enum': [1, 2]}",
        "3",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': '3'.','line':1,'column':1}")]

    // pattern validation
    [InlineData("{'pattern': '^a.*'}", "'a'", "")]
    [InlineData(
        "{'pattern': '^a.*'}",
        "'b'",
        "{'message_severity':'warning','code':'format-invalid','message':'String 'b' is not a valid '^a.*'.','line':1,'column':3}")]

    // string length validation
    [InlineData("{'type': 'string', 'minLength': 1, 'maxLength': 5}", "'a'", "")]
    [InlineData("{'type': 'string', 'maxLength': 1}", "'1963-06-19T08:30:06Z'",
        "{'message_severity':'warning','code':'string-length-invalid','message':'String '' is too long: 20 characters. Length should be <= 1.','line':1,'column':22}")]
    [InlineData("{'properties': {'str': {'minLength': 1, 'maxLength': 5}}}", "{'str': null}", "")]
    [InlineData("{'type': 'string', 'minLength': 1}", "''",
        "{'message_severity':'warning','code':'string-length-invalid','message':'String '' is too short: 0 characters. Length should be >= 1.','line':1,'column':2}")]
    [InlineData("{'type': 'string', 'maxLength': 1}", "'ab'",
        "{'message_severity':'warning','code':'string-length-invalid','message':'String '' is too long: 2 characters. Length should be <= 1.','line':1,'column':4}")]
    [InlineData("{'properties': {'str': {'maxLength': 2, 'minLength': 4}}}", "{'str': 'abc'}",
        @"{'message_severity':'warning','code':'string-length-invalid','message':'String 'str' is too long: 3 characters. Length should be <= 2.','line':1,'column':13}
              {'message_severity':'warning','code':'string-length-invalid','message':'String 'str' is too short: 3 characters. Length should be >= 4.','line':1,'column':13}")]
    [InlineData(
        "{'properties': {'key':{'properties': {'str': {'maxLength': 2}}}}}",
        "{'key':{'str': 'abc'}}",
        "{'message_severity':'warning','code':'string-length-invalid','message':'String 'key.str' is too long: 3 characters. Length should be <= 2.','line':1,'column':20}")]

    // number validation
    [InlineData("{'minimum': 1, 'maximum': 1}", "1", "")]
    [InlineData("{'exclusiveMinimum': 0.99, 'exclusiveMaximum': 1.01}", "1", "")]
    [InlineData("{'minimum': 100, 'maximum': -100}", "1",
        @"{'message_severity':'warning','code':'number-invalid','message':'Number '1' should be <= -100.','line':1,'column':1}
              {'message_severity':'warning','code':'number-invalid','message':'Number '1' should be >= 100.','line':1,'column':1}")]
    [InlineData("{'exclusiveMinimum': 100, 'exclusiveMaximum': -100}", "1",
        @"{'message_severity':'warning','code':'number-invalid','message':'Number '1' should be < -100.','line':1,'column':1}
              {'message_severity':'warning','code':'number-invalid','message':'Number '1' should be > 100.','line':1,'column':1}")]

    [InlineData("{'multipleOf': 1}", "1", "")]
    [InlineData("{'multipleOf': 0}", "1", "")]
    [InlineData("{'multipleOf': 0.0}", "1", "")]
    [InlineData("{'multipleOf': 2}", "1",
        "{'message_severity':'warning','code':'number-invalid','message':'Number '1' should be multiple of 2.','line':1,'column':1}")]

    // string format validation
    [InlineData("{'type': ['string'], 'format': 'date-time'}", "'1963-06-19T08:30:06Z'", "")]
    [InlineData("{'type': ['string', 'number'], 'format': 'date-time'}", "1", "")]
    [InlineData("{'type': ['string'], 'format': 'date-time'}", "'invalid'",
        "{'message_severity':'warning','code':'format-invalid','message':'String 'invalid' is not a valid 'DateTime'.','line':1,'column':9}")]

    [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19'", "")]
    [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-13-99'",
        "{'message_severity':'warning','code':'format-invalid','message':'String '1963-13-99' is not a valid 'Date'.','line':1,'column':12}")]
    [InlineData("{'type': ['string'], 'format': 'date'}", "'1963-06-19T08:30:06Z'",
        "{'message_severity':'warning','code':'format-invalid','message':'String '1963-06-19T08:30:06Z' is not a valid 'Date'.','line':1,'column':22}")]

    [InlineData("{'type': ['string'], 'format': 'time'}", "'08:30:06Z'", "")]
    [InlineData("{'type': ['string'], 'format': 'time'}", "'1963-06-19T08:30:06Z'",
        "{'message_severity':'warning','code':'format-invalid','message':'String '1963-06-19T08:30:06Z' is not a valid 'Time'.','line':1,'column':22}")]

    // properties validation
    [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 'value'}", "")]
    [InlineData("{'properties': {'key': {'type': 'string'}}}", "{'key': 1}",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'String' but got 'Integer'.','line':1,'column':9}")]

    // additional properties validation
    // AdditionalProperty is enabled with explicit false
    [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {}}", "{'key': 'value', 'key1': 'value1'}", "")]
    [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': null}", "{'key': 'value', 'key1': 'value1'}", "")]
    [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': false}", "{'key': 'value', 'key1': 'value1'}",
        "{'message_severity':'warning','code':'unknown-field','message':'Could not find member 'key1' on object of type 'String'.','line':1,'column':33}")]
    [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'number'}}", "{'key': 'value', 'key1': 'value1'}",
        "{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'Number' but got 'String'.','line':1,'column':33}")]
    [InlineData("{'properties': {'key': {'type': 'string'}}, 'additionalProperties': {'type': 'string', 'enum': ['a']}}", "{'key': 'value', 'key1': 'value1'}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': 'value1'.','line':1,'column':33}")]

    // property name validation
    [InlineData("{'propertyNames': {'maxLength': 1}}", "{'a': 0}", "")]
    [InlineData("{'propertyNames': {'maxLength': 1}}", "{'ab': 0}",
        "{'message_severity':'warning','code':'string-length-invalid','message':'String 'ab' is too long: 2 characters. Length should be <= 1.','line':1,'column':6}")]

    // property count validation
    [InlineData("{'maxProperties': 3}", "{}", "")]
    [InlineData("{'maxProperties': 0}", "{'key': 0}",
        "{'message_severity':'warning','code':'property-count-invalid','message':'Object '' property count should be <= 0.','line':1,'column':1}")]
    [InlineData("{'minProperties': 1}", "{}",
        "{'message_severity':'warning','code':'property-count-invalid','message':'Object '' property count should be >= 1.','line':1,'column':1}")]
    [InlineData("{'maxProperties': 0, 'minProperties': 4}", "{'key': 0}",
        @"{'message_severity':'warning','code':'property-count-invalid','message':'Object '' property count should be <= 0.','line':1,'column':1}
              {'message_severity':'warning','code':'property-count-invalid','message':'Object '' property count should be >= 4.','line':1,'column':1}")]

    // array validation
    [InlineData("{'items': {'type': 'string'}}", "['a','b']", "")]
    [InlineData("{'items': {'type': 'boolean'}}", "['a','b']",
        @"{'message_severity':'warning','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','line':1,'column':4}
              {'message_severity':'warning','code':'unexpected-type','message':'Expected type 'Boolean' but got 'String'.','line':1,'column':8}")]

    [InlineData("{'maxItems': 3, 'minItems': 1}", "['a','b']", "")]
    [InlineData("{'properties': {'arr': {'maxItems': 3, 'minItems': 1}}}", "{'arr': ['a','b','c','d']}",
        "{'message_severity':'warning','code':'array-length-invalid','message':'Array 'arr' length should be <= 3.','line':1,'column':9}")]
    [InlineData("{'maxItems': 3, 'minItems': 1}", "[]",
        "{'message_severity':'warning','code':'array-length-invalid','message':'Array '' length should be >= 1.','line':1,'column':1}")]
    [InlineData("{'maxItems': 2, 'minItems': 4}", "['a','b','c']",
        @"{'message_severity':'warning','code':'array-length-invalid','message':'Array '' length should be <= 2.','line':1,'column':1}
              {'message_severity':'warning','code':'array-length-invalid','message':'Array '' length should be >= 4.','line':1,'column':1}")]
    [InlineData("{'items': {'type':'string', 'enum':['a', 'b']}}", "['a','b']", "")]
    [InlineData("{'items': {'type':'string', 'enum':['a', 'b']}}", "['a','c']",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': 'c'.','line':1,'column':8}")]

    // uniqueItems validation
    [InlineData("{'uniqueItems': true}", "[1, 2]", "")]
    [InlineData("{'uniqueItems': true}", "[1, 1]",
        @"{'message_severity':'warning','code':'array-not-unique','message':'Array '' items should be unique.','line':1,'column':1}")]

    // contains validation
    [InlineData("{'contains': {'const': 1}}", "[1]", "")]
    [InlineData("{'contains': {'const': 1}}", "[2]",
        @"{'message_severity':'warning','code':'array-contains-failed','message':'Array '' should contain at least one item that matches JSON schema.','line':1,'column':1}")]

    // additionalItems validation
    [InlineData("{'items': [{'const': 1}], 'additionalItems': true}", "[1]", "")]
    [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1]", "")]
    [InlineData("{'items': [{'const': 1}], 'additionalItems': false}", "[1, 2]",
        "{'message_severity':'warning','code':'array-length-invalid','message':'Array '' length should be <= 1.','line':1,'column':1}")]
    [InlineData("{'items': [{'const': 1}], 'additionalItems': {'const': 2}}", "[1, 3]",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for '': '3'.','line':1,'column':5}")]

    // required validation
    [InlineData("{'required': []}", "{}", "")]
    [InlineData("{'required': ['a']}", "{'a': 1}", "")]
    [InlineData("{'required': ['a']}", "{'b': 1}",
        "{'message_severity':'warning','code':'missing-attribute','message':'Missing required attribute: 'a'.','line':1,'column':1}")]

    // boolean schema
    [InlineData("true", "[]", "")]
    [InlineData("false", "[]",
        "{'message_severity':'warning','code':'boolean-schema-failed','message':'Boolean schema validation failed for ''.','line':1,'column':1}")]

    // dependencies validation
    [InlineData("{'dependencies': {}}", "{}", "")]
    [InlineData("{'dependencies': {'key1': ['key2']}}", "{'key1' : 1}",
        "{'message_severity':'warning','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','line':1,'column':1}")]

    // dependencies as schema
    [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a', 'key2': 'b'}", "")]
    [InlineData("{'dependencies': {'key1': {'required': ['key2']}}}", "{'key1': 'a'}",
        "{'message_severity':'warning','code':'dependent-schemas-failed','message':'DependentSchemas validation failed for attribute: 'key1'.','line':1,'column':12}")]

    // dependentSchemas validation
    [InlineData("{'dependentSchemas': {}}", "{}", "")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}}", "{'key1' : 1, 'key2' : 2}", "")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}}", "{}", "")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}}", "{'key1' : 1}",
        "{'message_severity':'warning','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','line':1,'column':1}")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}}", "{'key1' : '1', 'key2': null}",
        "{'message_severity':'warning','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','line':1,'column':1}")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}}", "{'key1' : '1', 'key2': ''}",
        "{'message_severity':'warning','code':'missing-paired-attribute','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','line':1,'column':1}")]
    [InlineData("{'properties': {'keys': {'dependentSchemas': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1, 'key2': 2}}", "")]
    [InlineData("{'properties': {'keys': {'dependentSchemas': {'key1': ['key2']}}}}", "{'keys' : {'key1' : 1}}",
        "{'message_severity':'warning','code':'missing-paired-attribute','message':'Missing attribute: 'keys.key2'. If you specify 'keys.key1', you must also specify 'keys.key2'.','line':1,'column':11}")]

    // dependentSchemas as schema
    [InlineData("{'dependentSchemas': {'key1': {'required': ['key2']}}}", "{}", "")]
    [InlineData("{'dependentSchemas': {'key1': {'required': ['key2']}}}", "{'key1': 'a', 'key2': 'b'}", "")]
    [InlineData("{'dependentSchemas': {'key1': {'required': ['key2']}}}", "{'key1': 'a'}",
        "{'message_severity':'warning','code':'dependent-schemas-failed','message':'DependentSchemas validation failed for attribute: 'key1'.','line':1,'column':12}")]

    // either validation
    [InlineData("{'either': []}", "{}", "")]
    [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1}", "")]
    [InlineData("{'either': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}", "")]
    [InlineData("{'either': [['key1', 'key2']]}", "{}",
        "{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','line':1,'column':1}")]
    [InlineData("{'either': [['key1', 'key2']]}", "{'key1': null}",
        "{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','line':1,'column':1}")]
    [InlineData("{'either': [['key1', 'key2']]}", "{'key1': ''}",
        "{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','line':1,'column':1}")]
    [InlineData("{'either': [['key1', 'key2', 'key3']]}", "{}",
        "{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2', 'key3'.','line':1,'column':1}")]
    [InlineData("{'either': [['key1', 'key2'], ['key3', 'key4']]}", "{}",
        @"{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','line':1,'column':1}
              {'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key3', 'key4'.','line':1,'column':1}")]
    [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
    [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}", "")]
    [InlineData("{'properties': {'keys': {'either': [['key1', 'key2']]}}}", "{'keys' : {}}",
        "{'message_severity':'warning','code':'missing-either-attribute','message':'One of the following attributes is required: 'key1', 'key2'.','line':1,'column':11}")]

    // precludes validation
    [InlineData("{'precludes': []}", "{}", "")]
    [InlineData("{'precludes': [['key1', 'key2']]}", "{}", "")]
    [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1}", "")]
    [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': null}", "")]
    [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': '1', 'key2': ''}", "")]
    [InlineData("{'precludes': [['key1', 'key2']]}", "{'key1': 1, 'key2': 2}",
        "{'message_severity':'warning','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','line':1,'column':1}")]
    [InlineData("{'precludes': [['key1', 'key2', 'key3']]}", "{'key1': 1, 'key2': 2, 'key3': 3}",
        "{'message_severity':'warning','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2', 'key3'.','line':1,'column':1}")]
    [InlineData("{'precludes': [['key1', 'key2'], ['key3', 'key4']]}", "{'key1': 1, 'key2': 2, 'key3': 3, 'key4': 4}",
        @"{'message_severity':'warning','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','line':1,'column':1}
              {'message_severity':'warning','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key3', 'key4'.','line':1,'column':1}")]
    [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {}}", "")]
    [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1}}", "")]
    [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
        "{'message_severity':'warning','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','line':1,'column':11}")]

    // date format validation
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{}", "")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': null}", "")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': ''}", "")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': '04/26/2019'}", "")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}", "{'key1': 'Dec 5 2018'}",
        "{'message_severity':'warning','code':'date-format-invalid','message':'Invalid date format for 'key1': 'Dec 5 2018'.','line':1,'column':21}")]

    [InlineData(
        "{'properties':{ 'key':{'properties': {'key1': {'dateFormat': 'M/d/yyyy'}}}}}",
        "{'key': {'key1': 'Dec 5 2018'}}",
        "{'message_severity':'warning','code':'date-format-invalid','message':'Invalid date format for 'key.key1': 'Dec 5 2018'.','line':1,'column':29}")]

    // date range validation
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-10000000:00:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}", "")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/2019'}",
        "{'message_severity':'warning','code':'date-out-of-range','message':'Value out of range for 'key1': '04/26/2019'.','line':1,'column':21}")]
    [InlineData("{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}", "{'key1': '04/26/4019'}",
        "{'message_severity':'warning','code':'date-out-of-range','message':'Value out of range for 'key1': '04/26/4019'.','line':1,'column':21}")]

    [InlineData(
        "{'properties':{ 'key':{'properties': {'key1': {'dateFormat': 'M/d/yyyy', 'relativeMinDate': '-2:00:00', 'relativeMaxDate': '5:00:00:00'}}}}}",
        "{'key': {'key1': '04/26/4019'}}",
        "{'message_severity':'warning','code':'date-out-of-range','message':'Value out of range for 'key.key1': '04/26/4019'.','line':1,'column':29}")]

    // deprecated validation
    [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{}", "")]
    [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': null}", "")]
    [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': ''}", "")]
    [InlineData("{'properties': {'key1': {'replacedBy': ''}}}", "{'key1': 1}",
        "{'message_severity':'warning','code':'attribute-deprecated','message':'Deprecated attribute: 'key1'.','line':1,'column':10}")]
    [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}}", "{'key1': 1}",
        "{'message_severity':'warning','code':'attribute-deprecated','message':'Deprecated attribute: 'key1', use 'key2' instead.','line':1,'column':10}")]

    [InlineData(
        "{'properties':{ 'key':{'properties': {'key1': {'replacedBy': 'key2'}}}}}",
        "{'key': {'key1': 1}}",
        "{'message_severity':'warning','code':'attribute-deprecated','message':'Deprecated attribute: 'key.key1', use 'key2' instead.','line':1,'column':18}")]

    // enum dependencies validation
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer'}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'key2' is not valid with 'key1' value 'yammer'.','line':1,'column':1}")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'': null, 'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer'}", "")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'': null, 'tabs': null, 'vba': null}}}}}", "{}", "")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': ['null', 'string']}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': null}", "")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'tabs'}", "")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': null, 'yammer': null}}}", "{'key1': 'yammer'}", "")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'value', 'key2': 'tabs'}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for 'key1': 'value'.','line':1,'column':16}")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'value', 'key2': 'tabs'}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for 'key1': 'value'.','line':1,'column':16}")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'.','line':1,'column':32}")]
    [InlineData("{'properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1[0]': {'.net': {'key2[0]': {'csharp': null, 'devlang': null}}, 'yammer': {'key2[0]': {'tabs': null, 'vba': null}}}}}", "{'key1': 'yammer', 'key2': 'abc'}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key2': 'abc' is not valid with 'key1' value 'yammer'.','line':1,'column':32}")]
    [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['yammer','abc']}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key1[1]': 'abc' is not valid with 'key1[0]' value 'yammer'.','line':1,'column':24}")]
    [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','vst']}", "")]
    [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': {'key1[2]': {'vst': null, 'yiu': null}}, 'vba': null}}}}}", "{'key1': ['yammer','tabs','abc']}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key1[2]': 'abc' is not valid with 'key1[1]' value 'tabs'.','line':1,'column':31}")]
    [InlineData("{'properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}", "{'key1': ['value','tabs']}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for 'key1[0]': 'value'.','line':1,'column':17}")]

    [InlineData(
        "{'properties': {'key': {'type': 'object','properties': {'key1': {'type': 'string'}, 'key2': {'type': 'string'}}, 'enumDependencies': {'key1': {'.net': {'key2': {'csharp': null, 'devlang': null}}, 'yammer': {'key2': {'tabs': null, 'vba': null}}}}}}}",
        "{'key': {'key1': 'yammer', 'key2': 'abc'}}",
        "{'message_severity':'warning','code':'invalid-paired-attribute','message':'Invalid value for 'key.key2': 'abc' is not valid with 'key.key1' value 'yammer'.','line':1,'column':40}")]
    [InlineData(
        "{'properties': {'key':{'type': 'object','properties': {'key1': {'type': 'array', 'items': {'type': 'string'}}}, 'enumDependencies': {'key1[0]': {'.net': {'key1[1]': {'csharp': null, 'devlang': null}}, 'yammer': {'key1[1]': {'tabs': null, 'vba': null}}}}}}}",
        "{'key': {'key1': ['value','tabs']}}",
        "{'message_severity':'warning','code':'invalid-value','message':'Invalid value for 'key.key1[0]': 'value'.','line':1,'column':25}")]

    // custom errors
    [InlineData("{'required': ['author'], 'rules': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
        "{'message_severity':'suggestion','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','line':1,'column':1}")]
    [InlineData("{'required': ['author'], 'rules': {'author': {'missing-attribute': {'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.'}}}}", "{'b': 1}",
        "{'message_severity':'warning','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','line':1,'column':1}")]
    [InlineData("{'properties': {'key1': {'replacedBy': 'key2'}}, 'rules': {'key1': {'attribute-deprecated': {'severity': 'suggestion', 'code': 'key1-attribute-deprecated'}}}}", "{'key1': 1}",
        "{'message_severity':'suggestion','code':'key1-attribute-deprecated','message':'Deprecated attribute: 'key1', use 'key2' instead.','line':1,'column':10}")]
    [InlineData("{'properties': {'keys': {'precludes': [['key1', 'key2']]}}, 'rules': {'keys.key1': {'precluded-attributes': {'severity': 'error'}}}}", "{'keys' : {'key1': 1, 'key2': 2}}",
        "{'message_severity':'error','code':'precluded-attributes','message':'Only one of the following attributes can exist: 'key1', 'key2'.','line':1,'column':11}")]
    [InlineData("{'dependentSchemas': {'key1': ['key2']}, 'rules': {'key1': {'missing-paired-attribute': {'code': 'key2-missing'}}}}", "{'key1' : 1}",
        "{'message_severity':'warning','code':'key2-missing','message':'Missing attribute: 'key2'. If you specify 'key1', you must also specify 'key2'.','line':1,'column':1}")]
    [InlineData("{'required': ['author'], 'rules': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.', 'pullRequestOnly': true}}}}", "{'b': 1}",
        "{'message_severity':'suggestion','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','line':1,'column':1}")]
    [InlineData("{'required': ['author'], 'rules': {'author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.', 'addOnly': true}}}}", "{'b': 1}",
        "{'message_severity':'suggestion','code':'author-missing','message':'Missing required attribute: 'author'. Add a valid GitHub ID.','line':1,'column':1}")]
    [InlineData(
        "{'properties': {'key':{'required': ['author'],'properties': {'author': {'type': ['string']}}}},'rules': {'key.author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.', 'pullRequestOnly': true}}}}",
        "{'key': {'b': 1}}",
        "{'message_severity':'suggestion','code':'author-missing','message':'Missing required attribute: 'key.author'. Add a valid GitHub ID.','line':1,'column':9}")]
    [InlineData(
        "{'properties': {'key':{'required': ['author'],'properties': {'author': {'type': ['string']}}}},'rules': {'key.author': {'missing-attribute': {'severity': 'suggestion', 'code': 'author-missing', 'additionalMessage': 'Add a valid GitHub ID.', 'addOnly': true}}}}",
        "{'key': {'b': 1}}",
        "{'message_severity':'suggestion','code':'author-missing','message':'Missing required attribute: 'key.author'. Add a valid GitHub ID.','line':1,'column':9}")]

    // strict required validation
    [InlineData("{'strictRequired': ['key1']}", "{'key1': 'a'}", "")]
    [InlineData("{'strictRequired': ['key1']}", "{}",
        "{'message_severity':'warning','code':'missing-attribute','message':'Missing required attribute: 'key1'.','line':1,'column':1}")]
    [InlineData("{'strictRequired': ['key1']}", "{'key1': null}",
        "{'message_severity':'warning','code':'missing-attribute','message':'Missing required attribute: 'key1'.','line':1,'column':1}")]
    [InlineData("{'strictRequired': ['key1']}", "{'key1': ''}",
        "{'message_severity':'warning','code':'missing-attribute','message':'Missing required attribute: 'key1'.','line':1,'column':1}")]

    [InlineData(
        "{'properties': {'key':{'strictRequired': ['key1'],'properties': {'key1': {'type': ['string']}}}}}",
        "{'key':{'key1': ''}}",
        "{'message_severity':'warning','code':'missing-attribute','message':'Missing required attribute: 'key.key1'.','line':1,'column':8}")]

    public void TestJsonSchemaValidation(string schema, string json, string expectedErrors)
    {
        var propertiesToCompare = new[] { "message_severity", "code", "message", "line", "column" };
        var jsonSchema = s_jsonSchemaLoader.LoadSchema(schema.Replace('\'', '"'));
        var payload = JsonUtility.Parse(new ErrorList(), json.Replace('\'', '"'), new("file"));
        var errors = new JsonSchemaValidator(jsonSchema).Validate(payload, new("file"));
        var expected = string.Join('\n', expectedErrors.Split('\n').Select(err => err.Trim()));
        var actual = string.Join(
            '\n',
            errors.Select(err =>
            {
                var obj = new JObject(JObject.Parse(err.ToString()).Properties().Where(property => propertiesToCompare.Contains(property.Name)));
                return obj.ToString(Formatting.None).Replace('"', '\'');
            }).OrderBy(err => err).ToArray());

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
        var jsonSchema = s_jsonSchemaLoader.LoadSchema(schema.Replace('\'', '"'));
        var payloads = Enumerable.Range(0, jsons.Length).Select(i => (meta: JsonUtility.Parse(new ErrorList(), jsons[i].Replace('\'', '"'), new($"file{i + 1}")), filepath: new FilePath($"file{i + 1}")));
        var jsonSchemaValidator = new JsonSchemaValidator(jsonSchema, null);

        foreach (var (meta, filepath) in payloads)
        {
            jsonSchemaValidator.Validate(meta, filepath);
        }

        var errors = jsonSchemaValidator.PostValidate();
        Assert.Equal(errorCount, errors.Count);
    }

    private static string FetchRemoteSchema(Uri baseUrl, Uri refUrl)
    {
        if (refUrl.GetLeftPart(UriPartial.Authority) == "http://localhost:1234")
        {
            return File.ReadAllText(Path.Combine("data/jschema/remotes", refUrl.LocalPath.TrimStart('/')));
        }
        return null;
    }
}
