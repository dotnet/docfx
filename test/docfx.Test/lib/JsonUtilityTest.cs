// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                    }
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

        [Fact]
        public void TestNull()
        {
            string json = null;
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
        [InlineData(@"{""mismatchType"": ""name""}", 1, 16, ErrorLevel.Warning, "invalid-schema")]
        [InlineData(@"{
""ValueBasic"":
  {""B"": 1,
  ""C"": ""c"",
  ""E"": ""e""}}", 5, 6, ErrorLevel.Warning, "invalid-schema")]
        [InlineData(@"{
""Items"":
  [{ ""B"": 1,
    ""C"": ""c"",
    ""E"": ""e""}]}", 5, 8, ErrorLevel.Warning, "invalid-schema")]
        [InlineData(@"{
""AnotherItems"":
  [{ ""F"": 1,
    ""G"": ""c"",
    ""E"": ""e""}]}", 5, 8, ErrorLevel.Warning, "invalid-schema")]
        internal void TestMismatchingFieldType(string json, int expectedLine, int expectedColumn, ErrorLevel expectedErrorLevel, string expectedErrorCode)
        {
            var (errors, result) = JsonUtility.Deserialize<ClassWithMoreMembers>(json);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedLine, error.Line);
                Assert.Equal(expectedColumn, error.Column);
            });
        }

        [Fact]
        public void TestMultipleMismatchingFieldType()
        {
            var yaml = @"{""mismatchType1"": ""name"",
""mismatchType2"": ""name""}";
            var (errors, result) = YamlUtility.Deserialize<BasicClass>(yaml);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("invalid-schema", error.Code);
                Assert.Equal(1, error.Line);
                Assert.Equal(2, error.Column);
                Assert.Equal("(Line: 1, Character: 2) Could not find member 'mismatchType1' on object of type 'BasicClass'", error.Message);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("invalid-schema", error.Code);
                Assert.Equal(2, error.Line);
                Assert.Equal(1, error.Column);
                Assert.Equal("(Line: 2, Character: 1) Could not find member 'mismatchType2' on object of type 'BasicClass'", error.Message);
            });
        }

        public class BasicClass
        {
            public string C { get; set; }

            public int B { get; set; }

            public bool D { get; set; }
        }

        public class AnotherBasicClass
        {
            public int F { get; set; }

            public string G { get; set; }

            public bool H { get; set; }
        }

        public class ClassWithReadOnlyField
        {
            public readonly string B;
        }

        public class ClassWithMoreMembers : BasicClass
        {
            public Dictionary<string, object> ValueDict { get; set; }

            public List<string> ValueList { get; set; }

            public BasicClass ValueBasic { get; set; }

            public List<BasicClass> Items { get; set; }

            public List<AnotherBasicClass> AnotherItems { get; set; }
        }
    }
}
