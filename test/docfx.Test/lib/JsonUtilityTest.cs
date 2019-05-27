// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class JsonUtilityTest
    {
        [Theory]
        [InlineData("", null)]
        [InlineData("[]", null)]
        [InlineData("{}", null)]
        [InlineData("true", null)]
        [InlineData(" { \"$schema\"  : \"schema\"", "schema")]
        [InlineData(" { \"$schema\": \"sche\"ma\"", "sche")]
        [InlineData(" { \"$schema\" : \"sche\\\"ma\"", "sche\"ma")]
        [InlineData(" { \"$schema\" : \"https://a.com/b.json\" }", "b")]
        public void TestReadMime(string input, string schema)
        {
            var result = JsonUtility.ReadMime(new StringReader(input), string.Empty);
            Assert.Equal(schema, result);
        }

        [Theory]
        [InlineData(" Add --globalMetadata, --globalMetadataFile and --fileMetadataFile\n")]
        [InlineData("\r\n Hello\n")]
        [InlineData("  \r\n Hello\n")]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData("FALSE")]
        [InlineData("Null")]
        [InlineData("null")]
        [InlineData("NULL")]
        public void TestObjectWithStringProperty(string input)
        {
            var sw = new StringWriter();
            JsonUtility.Serialize(sw, new BasicClass { C = input });
            var json = sw.ToString();
            var (errors, value) = DeserializeWithValidation<BasicClass>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(input, value.C);
        }

        [Fact]
        public void TestBasicClass()
        {
            var json = JsonUtility.Serialize(new BasicClass { B = 1, C = "Good!", D = true }, indent: true);
            Assert.Equal(
                @"{
  ""c"": ""Good!"",
  ""b"": 1,
  ""d"": true
}".Replace("\r\n", "\n"),
                json.Replace("\r\n", "\n"));
            var (errors, value) = DeserializeWithValidation<BasicClass>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.True(value.D);
        }

        [Fact]
        public void TestJsonDeserializeIsCaseSensitive()
        {
            var (errors, value) = DeserializeWithValidation<BasicClass>("{\"B\":1}");
            Assert.Equal(0, value.B);
        }

        [Fact]
        public void TestBasicClassWithNullCharactor()
        {
            var json = JsonUtility.Serialize(new BasicClass { C = null, });
            Assert.Equal("{\"b\":0,\"d\":false}", json);
            var (errors, value) = DeserializeWithValidation<BasicClass>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(0, value.B);
            Assert.Null(value.C);
            Assert.False(value.D);
        }

        [Fact]
        public void TestBoolean()
        {
            var sw = new StringWriter();
            JsonUtility.Serialize(sw, new object[] { true, false });
            var json = sw.ToString();
            Assert.Equal("[true,false]", json);
            var (errors, value) = DeserializeWithValidation<object[]>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(2, value.Length);
            Assert.True((bool)value[0]);
            Assert.False((bool)value[1]);
        }

        [Fact]
        public void TestListOfBasicClass()
        {
            var json = JsonUtility.Serialize(
                (from i in Enumerable.Range(0, 10)
                 select new BasicClass { B = i, C = $"Good{i}!", D = (i % 2 == 0) ? true : false }).ToList());
            var (errors, values) = DeserializeWithValidation<List<BasicClass>>(json);
            Assert.Empty(errors);
            Assert.NotNull(values);
            Assert.Equal(10, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(i, values[i].B);
                Assert.Equal($"Good{i}!", values[i].C);
                Assert.Equal((i % 2 == 0) ? true : false, values[i].D);
            }
        }

        [Fact]
        public void TestClassWithReadOnlyField()
        {
            var json = @"
{
    ""b"": ""test""
}";
            var (errors, value) = DeserializeWithValidation<ClassWithReadOnlyField>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal("test", value.B);
        }

        [Fact]
        public void TestClassWithMoreMembersBySerializeThenDeserialize()
        {
            var sw = new StringWriter();
            JsonUtility.Serialize(
                sw,
                new ClassWithMoreMembers
                {
                    D = true,
                    B = 1,
                    C = "Good!",
                    ValueDict = new Dictionary<string, object>
                    {
                        { "b", "valueA" },
                        { "c", 10 },
                        { "a", true }
                    },
                    ValueList = new List<string> { "b", "a", },
                    ValueBasic = new BasicClass
                    {
                        D = false,
                        B = 5,
                        C = "Amazing!",
                    },
                    ValueRequired = "a",
                }, indent: true);
            var json = sw.ToString();
            Assert.Equal(
                @"{
  ""valueDict"": {
    ""b"": ""valueA"",
    ""c"": 10,
    ""a"": true
  },
  ""valueList"": [
    ""b"",
    ""a""
  ],
  ""valueBasic"": {
    ""c"": ""Amazing!"",
    ""b"": 5,
    ""d"": false
  },
  ""valueRequired"": ""a"",
  ""c"": ""Good!"",
  ""b"": 1,
  ""d"": true
}".Replace("\r\n", "\n"),
                json.Replace("\r\n", "\n"));
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Empty(errors.Where(error => error.Level == ErrorLevel.Error));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.True(value.D);
            Assert.Equal(5, value.ValueBasic.B);
            Assert.Equal("Amazing!", value.ValueBasic.C);
            Assert.False(value.ValueBasic.D);
            Assert.True((bool)value.ValueDict["a"]);
            Assert.Equal("valueA", value.ValueDict["b"]);
            Assert.Equal((long)10, value.ValueDict["c"]);
            Assert.Equal("b", value.ValueList[0]);
            Assert.Equal("a", value.ValueList[1]);
        }

        [Theory]
        [InlineData("{'name':'title','items':[,{'name':'1'}]}", "'items' contains null value, the null value has been removed", "null-array-value", ErrorLevel.Warning)]
        [InlineData("[1,,1,1]", "'[1]' contains null value, the null value has been removed", "null-array-value", ErrorLevel.Warning)]
        internal void TestNulllValue(string json, string message, string errorCode, ErrorLevel errorLevel)
        {
            var (errors, result) = DeserializeWithValidation<JToken>(json.Replace('\'', '"'));
            Assert.Collection(errors, error =>
            {
                Assert.Equal(errorLevel, error.Level);
                Assert.Equal(errorCode, error.Code);
                Assert.Equal(message, error.Message);
            });
        }

        [Fact]
        public void TestEmptyString()
        {
            var json = string.Empty;
            var exception = Assert.Throws<DocfxException>(() => JsonUtility.Parse(json, null));
        }

        [Theory]
        [InlineData("1", 1, 1)]
        [InlineData(@"{""key"":""value""}", 1, 14)]
        [InlineData(@"{
""list"":[{item: 1}]}", 2, 8)]
        public void TestParsedJTokenHasLineInfo(string json, int expectedLine, int expectedColumn)
        {
            var value = JToken.Parse(json, new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load });

            // Get the first JValue of the first JProperty if any
            var lineInfo = (value.Children().Any() ? value.Children().First().Children().First() : value) as IJsonLineInfo;
            Assert.Equal(expectedLine, lineInfo.LineNumber);
            Assert.Equal(expectedColumn, lineInfo.LinePosition);
        }

        [Theory]
        [InlineData(@"{""mismatchField"": ""name"", ""valueRequired"": ""a""}", 1, 17, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"{
""anotherItems"":
  [{ ""f"": 1,
    ""g"": ""c"",
    ""e"": ""e""}], ""valueRequired"": ""a""}", 5, 8, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"{
""nestedItems"":
  [[{ ""f"": 1,
    ""g"": ""c"",
    ""e"": ""e""}]], ""valueRequired"": ""a""}", 5, 8, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"[{
""b"": 1,
""c"": ""c"",
""e"": ""e"",
""nestedSealedMember"": {""unknown"": 1}}]", 5, 33, ErrorLevel.Warning, "unknown-field", typeof(List<NotSealedClass>))]
        internal void TestUnknownFieldType(string json, int expectedLine, int expectedColumn, ErrorLevel expectedErrorLevel, string expectedErrorCode, Type type)
        {
            var (_, token) = JsonUtility.Parse(json, null);
            var (errors, result) = JsonUtility.ToObject(token, type);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedLine, error.Line);
                Assert.Equal(expectedColumn, error.Column);
            });
        }

        [Fact]
        public void TestMultipleUnknownFieldType()
        {
            var json = @"{""mismatchField1"": ""name"",
""mismatchField2"": ""name"",
""valueRequired"": ""a""}";
            var (errors, result) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(1, error.Line);
                Assert.Equal(18, error.Column);
                Assert.Equal("Could not find member 'mismatchField1' on object of type 'ClassWithMoreMembers'.", error.Message);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(2, error.Line);
                Assert.Equal(17, error.Column);
                Assert.Equal("Could not find member 'mismatchField2' on object of type 'ClassWithMoreMembers'.", error.Message);
            });
        }

        [Theory]
        [InlineData(@"{
'numberList':
  [1, 'a'],
'valueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 3, 9)]
        [InlineData(@"{'b' : 'b', 'valueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 1, 10)]
        [InlineData(@"{'valueEnum':'Four', 'valueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 1, 19)]
        internal void TestMismatchingPrimitiveFieldType(string json, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn)
        {
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json.Replace('\'', '\"'));
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedErrorLine, error.Line);
                Assert.Equal(expectedErrorColumn, error.Column);
            });
        }

        [Theory]
        [InlineData(@"{
""b"": 1,
""c"": ""c"",
""e"": ""e""}", typeof(NotSealedClass))]
        [InlineData(@"{
""data"":{
""b"": 1,
""c"": ""c"",
""e"": ""e""}}", typeof(SealedClassNestedWithNotSealedType))]
        [InlineData(@"{
""items"":[{
""b"": 1,
""c"": ""c"",
""e"": ""e""}]}", typeof(SealedClassNestedWithNotSealedType))]
        [InlineData(@"[{
""b"": 1,
""c"": ""c"",
""e"": ""e""}]", typeof(List<NotSealedClass>))]
        public void TestObjectTypeWithNotSealedType(string json, Type type)
        {
            var (_, token) = JsonUtility.Parse(json, null);
            var (errors, value) = JsonUtility.ToObject(token, type);
            Assert.Empty(errors);
        }

        [Fact]
        public void TestNestedObjectTypeWithNotSealedType()
        {
            var json = @"[{
""b"": 1,
""c"": ""c"",
""e"": ""e"",
""nestedSealedMember"": {""unknown"": 1}}]";
            var (errors, value) = DeserializeWithValidation<List<NotSealedClass>>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(5, error.Line);
                Assert.Equal(33, error.Column);
            });
        }

        [Theory]
        [InlineData(@"{""regPatternValue"":""3"", ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 22)]
        [InlineData(@"{""valueWithLengthRestriction"":""a"", ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 33)]
        [InlineData(@"{""valueWithLengthRestriction"":""abcd"", ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 36)]
        [InlineData(@"{""listValueWithLengthRestriction"":[], ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 35)]
        [InlineData(@"{""listValueWithLengthRestriction"":[""a"", ""b"", ""c"", ""d""], ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 35)]
        [InlineData(@"{""nestedMember"": {""valueWithLengthRestriction"":""abcd""}, ""valueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 53)]
        [InlineData(@"{""b"": 1}", ErrorLevel.Error, "violate-schema", 1, 1)]
        internal void TestSchemaViolation(string json, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn)
        {
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedErrorLine, error.Line);
                Assert.Equal(expectedErrorColumn, error.Column);
            });
        }

        [Fact]
        public void TestMultipleSchemaViolationForPrimitiveType()
        {
            var json = @"{
""numberList"": [1, ""a""],
""b"" : ""b"",
""valueEnum"":""Four"",
""valueRequired"": ""a""}";
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(2, error.Line);
                Assert.Equal(21, error.Column);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(3, error.Line);
                Assert.Equal(9, error.Column);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(4, error.Line);
                Assert.Equal(18, error.Column);
            });
        }

        [Fact]
        public void TestMultipleSchemaViolation()
        {
            var json = @"{
""regPatternValue"":""3"",
""valueWithLengthRestriction"":""a"",
""listValueWithLengthRestriction"":[],
""nestedMember"": {""valueWithLengthRestriction"":""abcd""},
""items"": ""notArray""}";
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(2, error.Line);
                Assert.Equal(21, error.Column);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(3, error.Line);
                Assert.Equal(32, error.Column);
                Assert.Equal("The field ValueWithLengthRestriction must be a string or array type with a minimum length of '2'.", error.Message);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(4, error.Line);
                Assert.Equal(34, error.Column);
                Assert.Equal("The field ListValueWithLengthRestriction must be a string or array type with a minimum length of '1'.", error.Message);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(5, error.Line);
                Assert.Equal(52, error.Column);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(6, error.Line);
                Assert.Equal(19, error.Column);
                Assert.Equal("Error converting value \"notArray\" to type 'System.Collections.Generic.List`1[Microsoft.Docs.Build.JsonUtilityTest+BasicClass]'.", error.Message);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(1, error.Line);
                Assert.Equal(1, error.Column);
                Assert.Equal("Required property 'valueRequired' not found in JSON.", error.Message);
            });
        }

        [Fact]
        public void TestConstraintFieldWithInconvertibleNestedType()
        {
            var json = @"{
""anotherItems"":[
    {
        ""items"": ""notArray"",
        ""h"": ""notBool""
    },
    {
        ""items"": []
    }
],
""valueRequired"": ""a""
}";
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(4, error.Line);
                Assert.Equal(27, error.Column);
                Assert.Equal("Error converting value \"notArray\" to type 'System.Collections.Generic.List`1[Microsoft.Docs.Build.JsonUtilityTest+BasicClass]'.", error.Message);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(5, error.Line);
                Assert.Equal(22, error.Column);
                Assert.Equal("Could not convert string to boolean: notBool.", error.Message);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(8, error.Line);
                Assert.Equal(18, error.Column);
                Assert.Equal("The field Items must be a string or array type with a minimum length of '1'.", error.Message);
            });
        }

        [Theory]
        [InlineData(@"{'b: 1 }")]
        [InlineData(@"{'b': 'not number'}")]
        public void SyntaxErrorShouldBeThrownWithoutSchemaValidation(string json)
        {
            var exception = Assert.Throws<DocfxException>(() => JsonUtility.Deserialize<BasicClass>(json.Replace('\'', '\"'), null));
            Assert.Equal("json-syntax-error", exception.Error.Code);
            Assert.Equal(ErrorLevel.Error, exception.Error.Level);
        }

        [Fact]
        public void OmitEmptyEnumerableValue()
        {
            var content = JsonUtility.Serialize(new EmptyEnumerable());
            Assert.Equal("{\"a\":\"\"}", content);
        }

        [Theory]
        [InlineData(
            "{\"key\":\"original\"}",
            "{\"key\":\"overwrite\"}",
            "{\"key\":\"overwrite\"}")]
        [InlineData(
            "{\"key\":[1,2,3]}",
            "{\"key\":[4,5,6]}",
            "{\"key\":[4,5,6]}")]
        [InlineData(
            "{\"key1\":\"value1\"}",
            "{\"key2\":\"value2\"}",
            "{\"key1\":\"value1\",\"key2\":\"value2\"}")]
        [InlineData("{'a':null}", "{'a':1}", "{'a':1}")]
        [InlineData("{'a':1}", "{'a':null}", "{'a':null}")]
        [InlineData("{}", "{'a':1}", "{'a':1}")]
        [InlineData("{}", "{'a':null}", "{'a':null}")]
        [InlineData("{'a':1}", "{}", "{'a':1}")]
        [InlineData("{'a':null}", "{}", "{'a':null}")]
        [InlineData("{'a':1}", "{'a':[]}", "{'a':[]}")]
        [InlineData("{'a':[1]}", "{'a':[2]}", "{'a':[2]}")]
        [InlineData("{'a':{'b':1}}", "{'a':{'b':{}}}", "{'a':{'b':{}}}")]
        [InlineData("{'a':{'b':1}}", "{'a':{'b':2}}", "{'a':{'b':2}}")]
        public void TestJsonMerge(string a, string b, string result)
        {
            var container = JObject.Parse(a.Replace('\'', '\"'));
            JsonUtility.Merge(container, JObject.Parse(b.Replace('\'', '\"')));
            Assert.Equal(result.Replace('\'', '\"'), container.ToString(Formatting.None));
        }

        [Fact]
        public void TestSerializeSourceInfoWithEmptyValue()
        {
            var basic = new BasicClass
            {
                B = 1,
                Property = new SourceInfo<string>(null, new SourceInfo(string.Empty, 0, 0)),
                Array = new SourceInfo<string[]>(new string[]{ }, new SourceInfo(string.Empty, 0, 0)),
                GenericArray = new SourceInfo<List<string>>(new List<string>(), new SourceInfo(string.Empty, 0, 0))
            };
            var result = JsonUtility.Serialize(basic);
            Assert.Equal("{\"b\":1,\"d\":false}", result);
        }

        /// <summary>
        /// Deserialize from yaml string, return error list at the same time
        /// </summary>
        private static (List<Error>, T) DeserializeWithValidation<T>(string input)
        {
            var (errors, token) = JsonUtility.Parse(input, null);
            var (mismatchingErrors, result) = JsonUtility.ToObject<T>(token);
            errors.AddRange(mismatchingErrors);
            return (errors, result);
        }

        public class EmptyEnumerable
        {
            public string A { get; set; } = "";
            public List<string> B { get; set; } = new List<string>();
            public IReadOnlyDictionary<string, object> C { get; set; } = new Dictionary<string, object>();
            public int[] D { get; set; } = new int[0];
            public IReadOnlyList<string> E { get; set; } = new List<string>();
            public IReadOnlyCollection<object> F { get; set; } = new List<object>();
            public Dictionary<string, Dictionary<string, object>> G = new Dictionary<string, Dictionary<string, object>>();
        }

        public class BasicClass
        {
            public string C { get; set; }

            public int B { get; set; }

            public bool D { get; set; }

            public SourceInfo<string> Property { get; set; }

            public SourceInfo<List<string>> GenericArray { get; set; }

            public SourceInfo<string[]> Array { get; set; }
        }

        public sealed class AnotherBasicClass
        {
            public int F { get; set; }

            public string G { get; set; }

            public bool H { get; set; }

            [MinLength(1)]
            public List<BasicClass> Items { get; set; }
        }

        public sealed class ClassWithReadOnlyField
        {
            public readonly string B;
        }

        public sealed class ClassWithMoreMembers : BasicClass
        {
            public Dictionary<string, object> ValueDict { get; set; }

            public List<string> ValueList { get; set; }

            public BasicClass ValueBasic { get; set; }

            public List<BasicClass> Items { get; set; }

            [MinLength(1)]
            public List<AnotherBasicClass> AnotherItems { get; set; }

            public List<List<AnotherBasicClass>> NestedItems { get; set; }

            public List<int> NumberList { get; set; }

            [RegularExpression("[a-z]")]
            public string RegPatternValue { get; set; }

            [MinLength(2), MaxLength(3)]
            public string ValueWithLengthRestriction { get; set; }

            [MinLength(1), MaxLength(3)]
            public List<string> ListValueWithLengthRestriction { get; set; }

            public NestedClass NestedMember { get; set; }

            // make it nullable, so that json serializer would not make a default value
            public BasicEnum? ValueEnum { get; set; }

            [JsonRequired]
            public string ValueRequired { get; set; }
        }

        public class NotSealedClass : BasicClass
        {
            public NestedClass NestedSealedMember { get; set; }
        }

        public sealed class SealedClassNestedWithNotSealedType : BasicClass
        {
            public NotSealedClass Data { get; set; }

            public List<NotSealedClass> Items { get; set; }
        }

        public sealed class NestedClass
        {
            [MinLength(2), MaxLength(3)]
            public string ValueWithLengthRestriction { get; set; }
        }

        public enum BasicEnum
        {
            One,
            Two,
            Three,
        }
    }
}
