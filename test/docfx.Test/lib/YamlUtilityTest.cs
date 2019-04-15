// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class YamlUtilityTest
    {
        [Theory]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("False")]
        [InlineData("false")]
        [InlineData("FALSE")]
        [InlineData("Null")]
        [InlineData("null")]
        [InlineData("NULL")]
        [InlineData("😄")]
        public void TestObjectWithStringProperty(string input)
        {
            var yaml = $"c: \"{input}\"";
            var (errors, value) = DeserializeWithValidation<BasicClass>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(input, value.C);
        }


        [Theory]
        [InlineData(
            @">
  this is multi-line string
  with folded style(>)",
            "this is multi-line string with folded style(>)")]
        [InlineData(
            @"|
  this is multi-line string
   Literal style(|)",
            @"this is multi-line string
 Literal style(|)")]
        [InlineData(
            @"this is multi-line string
  plain style",
            @"this is multi-line string plain style")]
        [InlineData(
            @"""this is multi-line string
  double-quoted style\
  with slash""",
            @"this is multi-line string double-quoted stylewith slash")]
        [InlineData(
            @"'this is multi-line string
  single-quoted style'",
            @"this is multi-line string single-quoted style")]
        public void TestObjectWithMultiLinesStringProperty(string input, string expected)
        {
            var yaml = $"c: {input}";
            var (errors, value) = DeserializeWithValidation<BasicClass>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(expected.Replace("\r\n", "\n"), value.C.Replace("\r\n", "\n"));
        }

        [Fact]
        public void TestBigInteger()
        {
            var yaml = @"
- 1234567890000
- 9876543210000
- 9223372036854775807
- 18446744073709551615
";
            var (errors, value) = DeserializeWithValidation<object[]>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(4, value.Length);
            Assert.Equal(1234567890000L, value[0]);
            Assert.Equal(9876543210000L, value[1]);
            Assert.Equal(long.MaxValue, value[2]);
            Assert.Equal(1.8446744073709552E+19, value[3]);
        }

        [Fact]
        public void TestNotprimitiveKey()
        {
            var yaml = @"
? - item1
  - item2
: value
";
            var exception = Assert.Throws<NotSupportedException>(() => YamlUtility.Parse(yaml, null));

            Assert.Equal("Not Supported: [ item1, item2 ] is not a primitive type", exception.Message);
        }

        [Fact]
        public void TestAnchor()
        {
            var yaml = @"
a: &anchor test
b: *anchor
";
            var (errors, value) = DeserializeWithValidation<Dictionary<string, string>>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal("test", value["a"]);
            Assert.Equal("test", value["b"]);
        }

        [Fact]
        public void TestBasicClass()
        {
            var yaml = @"
b: 1
c: Good!
d: true
";
            var (errors, value) = DeserializeWithValidation<BasicClass>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.True(value.D);
        }

        [Fact]
        public void TestBoolean()
        {
            var yaml = @"
- true
- false
";
            var (errors1, value) = DeserializeWithValidation<object[]>(yaml);
            Assert.Empty(errors1);
            Assert.NotNull(value);
            Assert.Equal(2, value.Count());
            Assert.True((bool)value[0]);
            Assert.False((bool)value[1]);
            var (errors2, value2) = YamlUtility.Parse(@"
- true
- True
- TRUE
- false
- False
- FALSE
", null);
            Assert.Empty(errors2);
            Assert.NotNull(value2);
            Assert.Equal(new[] { true, true, true, false, false, false }, value2.Select(j => (bool)j).ToArray());
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("Null")]
        [InlineData("NULL")]
        public void TestNull(string yaml)
        {
            var (errors, value) = DeserializeWithValidation<object>(yaml);
            Assert.Empty(errors);
            Assert.Null(value);
        }

        [Theory]
        [InlineData("Infinity", double.PositiveInfinity)]
        [InlineData("-Infinity", double.NegativeInfinity)]
        [InlineData("NaN", double.NaN)]
        public void TestSpecialDouble(string yaml, double expected)
        {
            var (errors, value) = DeserializeWithValidation<double>(yaml);
            Assert.Empty(errors);
            Assert.Equal(value, expected);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("### No-Yaml-Mime\r\n1\r\n...\r\n", null)]
        [InlineData("#YamlMime:a", "a")]
        [InlineData("###  YamlMime: LandingData ", "LandingData")]
        [InlineData("#YamlMime:Test-Yaml-Mime", "Test-Yaml-Mime")]
        public void YamlMime(string yaml, string mime)
        {
            Assert.Equal(mime, YamlUtility.ReadMime(yaml));
        }

        [Fact]
        public void TestListOfBasicClass()
        {
            var yaml = @"- c: Good0!
- b: 1
  c: Good1!
  d: true
- c: Good2!
  b: 2
  d: false
- d: true
  c: Good3!
  b: 3
- b: 4
  c: Good4!
  d: false
- b: 5
  c: Good5!
  d: true
- b: 6
  c: Good6!
  d: false
- b: 7
  c: Good7!
  d: true
- b: 8
  c: Good8!
  d: false
- b: 9
  c: Good9!
  d: true
";
            var (errors, values) = DeserializeWithValidation<List<BasicClass>>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(values);
            Assert.Equal(10, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(i, values[i].B);
                Assert.Equal($"Good{i}!", values[i].C);
                Assert.Equal((i % 2 != 0) ? true : false, values[i].D);
            }
        }

        [Fact]
        public void TestClassWithReadOnlyField()
        {
            var yaml = $"b: test";
            var (errors, value) = DeserializeWithValidation<ClassWithReadOnlyField>(yaml);
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal("test", value.B);
        }

        [Fact]
        public void TestClassWithMoreMembers()
        {
            var yaml = @"b: 1
c: Good1!
d: true
valueDict:
  keyA: 1
  keyB: Good2!
  keyC: true
valueList:
- ItemA
- ""True""
- ""3""
- ""ItemB""
valueBasic:
  b: 2
  c: Good3!
  d: false
valueRequired: a
";
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(yaml);
            Assert.Empty(errors.Where(error => error.Level == ErrorLevel.Error));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good1!", value.C);
            Assert.True(value.D);
            Assert.Equal((long)1, value.ValueDict["keyA"]);
            Assert.Equal("Good2!", value.ValueDict["keyB"]);
            Assert.True((bool)value.ValueDict["keyC"]);
            Assert.Equal("ItemA", value.ValueList[0]);
            Assert.Equal("True", value.ValueList[1]);
            Assert.Equal("3", value.ValueList[2]);
            Assert.Equal("ItemB", value.ValueList[3]);
            Assert.Equal((long)2, value.ValueBasic.B);
            Assert.Equal("Good3!", value.ValueBasic.C);
            Assert.False(value.ValueBasic.D);
        }

        [Fact]
        public void TestStringEmpty()
        {
            var yaml = String.Empty;
            var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(yaml);
            Assert.Empty(errors);
            Assert.Null(value);
        }

        [Fact]
        public void TestDuplicatedKeys()
        {
            var yaml = @"
Key1: 0
Key1: 0
";
            var exception = Assert.Throws<DocfxException>(() => YamlUtility.Parse(yaml, null));
            Assert.Contains("Key 'Key1' is already defined, remove the duplicate key", exception.Message);
        }

        [Theory]
        [InlineData("1", 1, 1)]
        [InlineData("name: name", 1, 7)]
        [InlineData(@"
items:
 - name: 1", 3, 2)]
        public void TestParsedJTokenHasLineInfo(string yaml, int expectedLine, int expectedColumn)
        {
            var (errors, value) = YamlUtility.Parse(yaml, null);
            Assert.Empty(errors);

            // Get the first JValue of the first JProperty if any
            var source = JsonUtility.GetSourceInfo((value.Children().Any() ? value.Children().First().Children().First() : value));
            Assert.Equal(expectedLine, source.Line);
            Assert.Equal(expectedColumn, source.Column);
        }

        [Theory]
        [InlineData(@"b: not number")]
        public void ThrowWithoutSchemaValidation(string yaml)
        {
            Assert.ThrowsAny<Exception>(() => YamlUtility.Deserialize<BasicClass>(yaml));
        }


        /// <summary>
        /// De-serialize a user input string to an object, return error list at the same time
        /// </summary>
        private static (List<Error> errors, T model) DeserializeWithValidation<T>(string json)
        {
            var (errors, token) = YamlUtility.Parse(json, null);
            var (mismatchingErrors, result) = JsonUtility.ToObject<T>(token);
            errors.AddRange(mismatchingErrors);
            return (errors, result);
        }

        public class BasicClass
        {
            public int B { get; set; }

            public string C { get; set; }

            public bool D { get; set; }
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
        }
    }
}
