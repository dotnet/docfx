// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var value = JsonUtility.Deserialize<BasicClass>(new StringReader(json));
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
            var value = JsonUtility.Deserialize<BasicClass>(new StringReader(json));
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
            var value = JsonUtility.Deserialize<BasicClass>(new StringReader(json));
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
            var value = JsonUtility.Deserialize<object[]>(new StringReader(json));
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
            var values = JsonUtility.Deserialize<List<BasicClass>>(new StringReader(json));
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
            var value = JsonUtility.Deserialize<ClassWithReadOnlyField>(new StringReader(json));
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
            var value = JsonUtility.Deserialize<ClassWithMoreMembers>(new StringReader(json));
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
            var targetJson = JsonUtility.Deserialize<JObject>(target);
            var sourceJson = JsonUtility.Deserialize<JObject>(source);
            var resultJson = JsonUtility.Merge(targetJson, sourceJson);
            var resultJsonString = JsonUtility.Serialize(resultJson);
            Assert.Equal(result, resultJsonString);
        }

        public class BasicClass
        {
            public string C { get; set; }

            public int B { get; set; }

            public bool D { get; set; }
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
        }
    }
}
