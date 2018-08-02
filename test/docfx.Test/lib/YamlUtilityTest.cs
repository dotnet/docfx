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
            var (errors, value) = YamlUtility.Deserialize<object[]>(yaml);
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
            var exception = Assert.Throws<NotSupportedException>(() => YamlUtility.Deserialize(yaml));

            Assert.Equal("Not Supported: [ item1, item2 ] is not a primitive type", exception.Message);
        }

        [Fact]
        public void TestAnchor()
        {
            var yaml = @"
A: &anchor test
B: *anchor
";
            var (errors, value) = YamlUtility.Deserialize<Dictionary<string, string>>(yaml);
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
            var (errors, value) = YamlUtility.Deserialize<Dictionary<string, object>>(yaml);
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
            var (errors, value) = YamlUtility.Deserialize<BasicClass>(yaml);
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
            var (errors1, value) = YamlUtility.Deserialize<object[]>(yaml);
            Assert.Empty(errors1);
            Assert.NotNull(value);
            Assert.Equal(2, value.Count());
            Assert.True((bool)value[0]);
            Assert.False((bool)value[1]);
            var (errors2, value2) = YamlUtility.Deserialize(@"
- true
- True
- TRUE
- false
- False
- FALSE
");
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
            var (errors, values) = YamlUtility.Deserialize<List<BasicClass>>(yaml);
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
            var (errors, value) = YamlUtility.Deserialize<ClassWithReadOnlyField>(yaml);
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
            var (errors, value) = YamlUtility.Deserialize<ClassWithMoreMembers>(yaml);
            Assert.Empty(errors.Where(error => error.Level == ErrorLevel.Error));
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
            var (errors, value) = YamlUtility.Deserialize<ClassWithMoreMembers>(yaml);
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
                Assert.Contains("'name' contains null value", error.Message);
                Assert.Equal(3, error.Line);
                Assert.Equal(5, error.Column);
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
                Assert.Contains("'items' contains null value", error.Message);
                Assert.Equal(3, error.Line);
                Assert.Equal(3, error.Column);
            });
        }

        [Theory]
        [InlineData("1", 1, 1)]
        [InlineData("name: name", 1, 7)]
        [InlineData(@"
items:
 - name: 1", 3, 2)]
        public void TestParsedJTokenHasLineInfo(string yaml, int expectedLine, int expectedColumn)
        {
            var (errors, value) = YamlUtility.Deserialize(yaml);
            Assert.Empty(errors);

            // Get the first JValue of the first JProperty if any
            var lineInfo = (value.Children().Any() ? value.Children().First().Children().First() : value) as IJsonLineInfo;
            Assert.Equal(expectedLine, lineInfo.LineNumber);
            Assert.Equal(expectedColumn, lineInfo.LinePosition);
        }

        [Theory]
        [InlineData("mismatchField: name", 1, 1, ErrorLevel.Warning, "unknown-field")]
        [InlineData(@"
        ValueBasic:
          B: 1
          C: c
          E: e", 5, 11, ErrorLevel.Warning, "unknown-field")]
        [InlineData(@"
        Items:
          - B: 1
            C: c
            E: e", 5, 13, ErrorLevel.Warning, "unknown-field")]
        [InlineData(@"
        AnotherItems:
          - H: 1
            G: c
            E: e", 5, 13, ErrorLevel.Warning, "unknown-field")]
        [InlineData(@"
        NestedItems:
          -
            - H: 1
              G: c
              E: e", 6, 15, ErrorLevel.Warning, "unknown-field")]
        internal void TestUnknownFieldType(string yaml, int expectedLine, int expectedColumn, ErrorLevel expectedErrorLevel, string expectedErrorCode)
        {
            var (errors, result) = YamlUtility.Deserialize<ClassWithMoreMembers>(yaml);
            Assert.Collection(errors, error =>
            {
                Assert.Equal(expectedErrorLevel, error.Level);
                Assert.Equal(expectedErrorCode, error.Code);
                Assert.Equal(expectedLine, error.Line);
                Assert.Equal(expectedColumn, error.Column);
            });
        }

        [Fact]
        public void TestMultipltUnknownFieldType()
        {
            var yaml = @"mismatchField1: name
mismatchField2: name";

            var (errors, result) = YamlUtility.Deserialize<BasicClass>(yaml);
            Assert.Collection(errors,
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(1, error.Line);
                Assert.Equal(1, error.Column);
                Assert.Equal("(Line: 1, Character: 1) Path:BasicClass.mismatchField1 Could not find member 'mismatchField1' on object of type 'BasicClass'", error.Message);
            },
            error =>
            {
                Assert.Equal(ErrorLevel.Warning, error.Level);
                Assert.Equal("unknown-field", error.Code);
                Assert.Equal(2, error.Line);
                Assert.Equal(1, error.Column);
                Assert.Equal("(Line: 2, Character: 1) Path:BasicClass.mismatchField2 Could not find member 'mismatchField2' on object of type 'BasicClass'", error.Message);
            });
        }

        [Theory]
        [InlineData(@"numberList:
        - 1
        - a", ErrorLevel.Error, "violate-schema", 3, 11)]
        [InlineData(@"
B: b", ErrorLevel.Error, "violate-schema", 2, 4)]
        [InlineData(@"ValueEnum: Four", ErrorLevel.Error, "violate-schema", 1, 12)]
        internal void TestMismatchingPrimitiveFieldType(string yaml, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn)
        {
            var ex = Assert.Throws<DocfxException>(() => YamlUtility.Deserialize<ClassWithMoreMembers>(yaml));
            Assert.Equal(expectedErrorLevel, ex.Error.Level);
            Assert.Equal(expectedErrorCode, ex.Error.Code);
            Assert.Equal(expectedErrorLine, ex.Error.Line);
            Assert.Equal(expectedErrorColumn, ex.Error.Column);
        }

        [Theory]
        [InlineData(@"
B: 1
C: c
E: e", typeof(ClassWithJsonExtensionData))]
        [InlineData(@"
Data: 
    B: 1
    C: c
    E: e", typeof(ClassWithNestedTypeContainsJsonExtensionData))]
        public void TestObjectTypeWithJsonExtensionData(string json, Type type)
        {
            var (_, token) = YamlUtility.Deserialize(json);
            var (errors, value) = JsonUtility.ToObject(token, type);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(@"regPatternValue: 3", ErrorLevel.Error, "violate-schema", 1, 18)]
        [InlineData(@"ValueWithLengthRestriction: a", ErrorLevel.Error, "violate-schema", 1, 29)]
        [InlineData(@"ValueWithLengthRestriction: abcd", ErrorLevel.Error, "violate-schema", 1, 29)]
        [InlineData(@"ListValueWithLengthRestriction: []", ErrorLevel.Error, "violate-schema", 1, 33)]
        [InlineData(@"ListValueWithLengthRestriction:
                        - a
                        - b
                        - c
                        - d", ErrorLevel.Error, "violate-schema", 2, 25)]
        [InlineData(@"NestedMember:
                        ValueWithLengthRestriction: abcd", ErrorLevel.Error, "violate-schema", 2, 53)]
        [InlineData(@"B: 1", ErrorLevel.Error, "violate-schema", 1, 1)]
        internal void TestSchemaViolation(string yaml, ErrorLevel expectedErrorLevel, string expectedErrorCode,
            int expectedErrorLine, int expectedErrorColumn)
        {
            var ex = Assert.Throws<DocfxException>(() => YamlUtility.Deserialize<ClassWithMoreMembers>(yaml));
            Assert.Equal(expectedErrorLevel, ex.Error.Level);
            Assert.Equal(expectedErrorCode, ex.Error.Code);
            Assert.Equal(expectedErrorLine, ex.Error.Line);
            Assert.Equal(expectedErrorColumn, ex.Error.Column);
        }

        public class BasicClass
        {
            public int B { get; set; }

            public string C { get; set; }

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

            public List<List<AnotherBasicClass>> NestedItems { get; set; }

            public List<int> NumberList { get; set; }

            [RegularExpression("[a-z]")]
            public string RegPatternValue { get; set; }

            [MinLength(2), MaxLength(3)]
            public string ValueWithLengthRestriction { get; set; }

            [MinLength(1), MaxLength(3)]
            public List<string> ListValueWithLengthRestriction { get; set; }

            public NestedClass NestedMember { get; set; }

            public BasicEnum ValueEnum { get; set; }

            [JsonRequired]
            public string ValueRequired { get; set; }
        }

        public class ClassWithJsonExtensionData : BasicClass
        {
            [JsonExtensionData]
            public JObject AdditionalData { get; set; }
        }

        public class ClassWithNestedTypeContainsJsonExtensionData : BasicClass
        {
            public ClassWithJsonExtensionData Data { get; set; }
        }

        public class NestedClass
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
