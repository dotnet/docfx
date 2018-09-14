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
            Assert.Equal(schema, JsonUtility.ReadMime(new StringReader(input)));
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
            var (errors, value) = JsonUtility.Deserialize<BasicClass>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(input, value.C);
        }

        [Fact]
        public void TestBasicClass()
        {
            var json = JsonUtility.Serialize(new BasicClass { B = 1, C = "Good!", D = true }, formatting: Formatting.Indented);
            Assert.Equal(
                @"{
  ""c"": ""Good!"",
  ""b"": 1,
  ""d"": true
}".Replace("\r\n", "\n"),
                json.Replace("\r\n", "\n"));
            var (errors, value) = JsonUtility.Deserialize<BasicClass>(json);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.True(value.D);
        }

        [Fact]
        public void TestBasicClassWithNullCharactor()
        {
            var json = JsonUtility.Serialize(new BasicClass { C = null, });
            Assert.Equal("{\"b\":0,\"d\":false}", json);
            var (errors, value) = JsonUtility.Deserialize<BasicClass>(json);
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
            var (errors, value) = JsonUtility.Deserialize<object[]>(json);
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
            var (errors, values) = JsonUtility.Deserialize<List<BasicClass>>(json);
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
            var (errors, value) = JsonUtility.Deserialize<ClassWithReadOnlyField>(json);
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
                }, formatting: Formatting.Indented);
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
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
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
        public void TestMerge(string target, string source, string result)
        {
            var (_, targetJson) = JsonUtility.Deserialize<JObject>(target);
            var (_, sourceJson) = JsonUtility.Deserialize<JObject>(source);
            var resultJson = JsonUtility.Merge(targetJson, sourceJson);
            var resultJsonString = JsonUtility.Serialize(resultJson);
            Assert.Equal(result, resultJsonString);
        }

        [Fact]
        public void TestListWithNullItem()
        {
            var json = "{\"name\":\"title\",\"items\":[,{\"name\":\"1\"}]}";
            var (errors, result) = JsonUtility.Deserialize<JToken>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Info, error.Level);
                Assert.Equal("null-value", error.Code);
                Assert.Contains("contains null value", error.Message);
            });
            var resultJsonString = JsonUtility.Serialize(result);
            Assert.Equal("{\"name\":\"title\",\"items\":[{\"name\":\"1\"}]}", resultJsonString);
        }

        [Fact]
        public void TestListItemWithNullValue()
        {
            var json = "{\"name\":\"title\",\"items\":[{\"name\":,\"displayName\":\"1\"}]}";
            var (errors, result) = JsonUtility.Deserialize<JToken>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Info, error.Level);
                Assert.Equal("null-value", error.Code);
                Assert.Contains("contains null value", error.Message);
            });
            var resultJsonString = JsonUtility.Serialize(result);
            Assert.Equal("{\"name\":\"title\",\"items\":[{\"displayName\":\"1\"}]}", resultJsonString);
        }

        [Fact]
        public void TestEmptyString()
        {
            var json = string.Empty;
            var exception = Assert.Throws<DocfxException>(() => JsonUtility.Deserialize(json));
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
        [InlineData(@"{""mismatchField"": ""name"", ""ValueRequired"": ""a""}", 1, 17, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"{
""AnotherItems"":
  [{ ""F"": 1,
    ""G"": ""c"",
    ""E"": ""e""}], ""ValueRequired"": ""a""}", 5, 8, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"{
""NestedItems"":
  [[{ ""F"": 1,
    ""G"": ""c"",
    ""E"": ""e""}]], ""ValueRequired"": ""a""}", 5, 8, ErrorLevel.Warning, "unknown-field", typeof(ClassWithMoreMembers))]
        [InlineData(@"[{
""B"": 1,
""C"": ""c"",
""E"": ""e"",
""NestedSealedMember"": {""Unknown"": 1}}]", 5, 33, ErrorLevel.Warning, "unknown-field", typeof(List<NotSealedClass>))]
        internal void TestUnknownFieldType(string json, int expectedLine, int expectedColumn, ErrorLevel expectedErrorLevel, string expectedErrorCode, Type type)
        {
            var (_, token) = JsonUtility.Deserialize(json);
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
""ValueRequired"": ""a""}";
            var (errors, result) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
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
'NumberList':
  [1, 'a'],
'ValueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 3, 9, "NumberList[1]")]
        [InlineData(@"{'B' : 'b', 'ValueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 1, 10, "B")]
        [InlineData(@"{'ValueEnum':'Four', 'ValueRequired': 'a'}", ErrorLevel.Error, "violate-schema", 1, 19, "ValueEnum")]
        internal void TestMismatchingPrimitiveFieldType(string json, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn, string expectedPath)
        {
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json.Replace('\'', '\"'));
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedErrorLine, error.Line);
                Assert.Equal(expectedErrorColumn, error.Column);
                Assert.Equal(expectedPath, error.JsonPath);
            });
        }

        [Theory]
        [InlineData(@"{
""B"": 1,
""C"": ""c"",
""E"": ""e""}", typeof(NotSealedClass))]
        [InlineData(@"{
""Data"":{
""B"": 1,
""C"": ""c"",
""E"": ""e""}}", typeof(SealedClassNestedWithNotSealedType))]
        [InlineData(@"{
""Items"":[{
""B"": 1,
""C"": ""c"",
""E"": ""e""}]}", typeof(SealedClassNestedWithNotSealedType))]
        [InlineData(@"[{
""B"": 1,
""C"": ""c"",
""E"": ""e""}]", typeof(List<NotSealedClass>))]
        public void TestObjectTypeWithNotSealedType(string json, Type type)
        {
            var (_, token) = JsonUtility.Deserialize(json);
            var (errors, value) = JsonUtility.ToObject(token, type);
            Assert.Empty(errors);
        }

        [Fact]
        public void TestNestedObjectTypeWithNotSealedType()
        {
            var yaml = @"[{
""B"": 1,
""C"": ""c"",
""E"": ""e"",
""NestedSealedMember"": {""Unknown"": 1}}]";
            var (errors, value) = JsonUtility.Deserialize<List<NotSealedClass>>(yaml);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(5, error.Line);
                Assert.Equal(33, error.Column);
            });
        }

        [Theory]
        [InlineData(@"{""regPatternValue"":""3"", ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 22, "regPatternValue")]
        [InlineData(@"{""valueWithLengthRestriction"":""a"", ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 33, "valueWithLengthRestriction")]
        [InlineData(@"{""valueWithLengthRestriction"":""abcd"", ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 36, "valueWithLengthRestriction")]
        [InlineData(@"{""listValueWithLengthRestriction"":[], ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 35, "listValueWithLengthRestriction")]
        [InlineData(@"{""listValueWithLengthRestriction"":[""a"", ""b"", ""c"", ""d""], ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 35, "listValueWithLengthRestriction")]
        [InlineData(@"{""nestedMember"": {""valueWithLengthRestriction"":""abcd""}, ""ValueRequired"": ""a""}", ErrorLevel.Error, "violate-schema", 1, 53, "nestedMember.valueWithLengthRestriction")]
        [InlineData(@"{""B"": 1}", ErrorLevel.Error, "violate-schema", 1, 1, "")]
        internal void TestSchemaViolation(string json, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn, string expectedPath)
        {
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedErrorLine, error.Line);
                Assert.Equal(expectedErrorColumn, error.Column);
                Assert.Equal(expectedPath, error.JsonPath);
            });
        }

        [Fact]
        public void TestMultipleSchemaViolationForPrimitiveType()
        {
            var json = @"{
""NumberList"": [1, ""a""],
""B"" : ""b"",
""ValueEnum"":""Four"",
""ValueRequired"": ""a""}";
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
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
""Items"": ""notArray""}";
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(6, error.Line);
                Assert.Equal(19, error.Column);
                Assert.Equal("Error converting value \"notArray\" to type 'System.Collections.Generic.List`1[Microsoft.Docs.Build.JsonUtilityTest+BasicClass]'.", error.Message);
                Assert.Equal("Items", error.JsonPath);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(1, error.Line);
                Assert.Equal(1, error.Column);
                Assert.Equal("Required property 'ValueRequired' not found in JSON.", error.Message);
                Assert.Equal("", error.JsonPath);
            }, error =>
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
                Assert.Equal("valueWithLengthRestriction", error.JsonPath);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(4, error.Line);
                Assert.Equal(34, error.Column);
                Assert.Equal("The field ListValueWithLengthRestriction must be a string or array type with a minimum length of '1'.", error.Message);
                Assert.Equal("listValueWithLengthRestriction", error.JsonPath);
            }, error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal("violate-schema", error.Code);
                Assert.Equal(5, error.Line);
                Assert.Equal(52, error.Column);
            });
        }

        [Fact]
        public void TestConstraintFieldWithInconvertibleNestedType()
        {
            var json = @"{
""AnotherItems"":[
    {
        ""Items"": ""notArray"",
        ""H"": ""notBool""
    },
    {
        ""Items"": []
    }
],
""ValueRequired"": ""a""
}";
            var (errors, value) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(4, error.Line);
                Assert.Equal(27, error.Column);
                Assert.Equal("Error converting value \"notArray\" to type 'System.Collections.Generic.List`1[Microsoft.Docs.Build.JsonUtilityTest+BasicClass]'.", error.Message);
                Assert.Equal("AnotherItems[0].Items", error.JsonPath);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(5, error.Line);
                Assert.Equal(22, error.Column);
                Assert.Equal("Could not convert string to boolean: notBool.", error.Message);
                Assert.Equal("AnotherItems[0].H", error.JsonPath);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Error, error.Level);
                Assert.Equal(8, error.Line);
                Assert.Equal(18, error.Column);
                Assert.Equal("The field Items must be a string or array type with a minimum length of '1'.", error.Message);
                Assert.Equal("AnotherItems[1].Items", error.JsonPath);
            });
        }

        public class BasicClass
        {
            public string C { get; set; }

            public int B { get; set; }

            public bool D { get; set; }
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
