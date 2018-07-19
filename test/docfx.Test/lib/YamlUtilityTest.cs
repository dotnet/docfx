// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using YamlDotNet.Core;

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
            var yaml = $"C: \"{input}\"";
            var (errors, value) = YamlUtility.Deserialize<BasicClass>(yaml);
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
            var yaml = $"C: {input}";
            var (errors, value) = YamlUtility.Deserialize<BasicClass>(yaml);
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
            var (errors, value) = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
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
            var exception = Assert.Throws<NotSupportedException>(() => YamlUtility.Deserialize(new StringReader(yaml)));

            Assert.Equal("Not Supported: [ item1, item2 ] is not a primitive type", exception.Message);
        }

        [Fact]
        public void TestAnchor()
        {
            var yaml = @"
A: &anchor test
B: *anchor
";
            var (errors, value) = YamlUtility.Deserialize<Dictionary<string, string>>(new StringReader(yaml));
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal("test", value["A"]);
            Assert.Equal("test", value["B"]);
        }

        [Fact]
        public void TestBasicClassWithNullCharactor()
        {
            var yaml = @"
C: ""~""
D: ~
";
            var (errors, value) = YamlUtility.Deserialize<Dictionary<string, object>>(new StringReader(yaml));
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Info, error.Level);
                Assert.Equal("null-value", error.Code);
                Assert.Contains("contains null value", error.Message);
            });
            Assert.NotNull(value);
            Assert.Equal("~", value["C"]);
            Assert.DoesNotContain("D", value.Keys);
        }

        [Fact]
        public void TestBasicClass()
        {
            var yaml = @"
B: 1
C: Good!
D: true
";
            var (errors, value) = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
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
            var (errors1, value) = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
            Assert.Empty(errors1);
            Assert.NotNull(value);
            Assert.Equal(2, value.Count());
            Assert.True((bool)value[0]);
            Assert.False((bool)value[1]);
            var (errors2, value2) = YamlUtility.Deserialize(new StringReader(@"
- true
- True
- TRUE
- false
- False
- FALSE
"));
            Assert.Empty(errors2);
            Assert.NotNull(value2);
            Assert.Equal(new[] { true, true, true, false, false, false }, value2.Select(j => (bool)j).ToArray());
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
            var yaml = @"- C: Good0!
- B: 1
  C: Good1!
  D: true
- C: Good2!
  B: 2
  D: false
- D: true
  C: Good3!
  B: 3
- B: 4
  C: Good4!
  D: false
- B: 5
  C: Good5!
  D: true
- B: 6
  C: Good6!
  D: false
- B: 7
  C: Good7!
  D: true
- B: 8
  C: Good8!
  D: false
- B: 9
  C: Good9!
  D: true
";
            var (errors, values) = YamlUtility.Deserialize<List<BasicClass>>(new StringReader(yaml));
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
            var yaml = $"B: test";
            var (errors, value) = YamlUtility.Deserialize<ClassWithReadOnlyField>(new StringReader(yaml));
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal("test", value.B);
        }

        [Fact]
        public void TestClassWithMoreMembers()
        {
            var yaml = @"B: 1
C: Good1!
D: true
ValueDict:
  KeyA: 1
  KeyB: Good2!
  KeyC: true
ValueList:
- ItemA
- ""True""
- ""3""
- ""ItemB""
ValueBasic:
  B: 2
  C: Good3!
  D: false
";
            var (errors, value) = YamlUtility.Deserialize<ClassWithMoreMembers>(new StringReader(yaml));
            Assert.Empty(errors);
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good1!", value.C);
            Assert.True(value.D);
            Assert.Equal((long)1, value.ValueDict["KeyA"]);
            Assert.Equal("Good2!", value.ValueDict["KeyB"]);
            Assert.True((bool)value.ValueDict["KeyC"]);
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
            var (errors, value) = YamlUtility.Deserialize<ClassWithMoreMembers>(new StringReader(yaml));
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
            var exception = Assert.Throws<DocfxException>(() => YamlUtility.Deserialize(yaml));
            Assert.Contains("Key 'Key1' is already defined, remove the duplicate key", exception.Message);
        }

        [Fact]
        public void TestListItemWithNullValue()
        {
            var yaml = @"name: List item with null value
items:
  - name:
    displayName: 1
";
            var (errors, value) = YamlUtility.Deserialize(yaml);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Info, error.Level);
                Assert.Equal("null-value", error.Code);
                Assert.Contains("name contains null value", error.Message);
            });
        }

        [Fact]
        public void TestListWithNullItem()
        {
            var yaml = @"name: List with null item
items:
  -
  - name: 1
";
            var (errors, value) = YamlUtility.Deserialize(yaml);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(ErrorLevel.Info, error.Level);
                Assert.Equal("null-value", error.Code);
                Assert.Contains("items contains null value", error.Message);
            });
        }

        public class BasicClass
        {
            public int B { get; set; }

            public string C { get; set; }

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
