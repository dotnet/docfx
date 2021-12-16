// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

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
        Assert.Empty(errors.ToArray());
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
        Assert.Empty(errors.ToArray());
        Assert.NotNull(value);
        Assert.Equal(expected.Replace("\r\n", "\n"), value.C.Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData("1234567890000", 1234567890000L)]
    [InlineData("9876543210000", 9876543210000L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("18446744073709551615", 1.8446744073709552E+19)]
    public void TestBigInteger(string yaml, object expected)
    {
        var (errors, actual) = DeserializeWithValidation<object>(yaml);
        Assert.Empty(errors.ToArray());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestDisallowThousands()
    {
        var yaml = "a: 123,456";
        var (errors, value) = DeserializeWithValidation<Dictionary<string, object>>(yaml);
        Assert.Empty(errors.ToArray());
        Assert.NotNull(value);
        Assert.Equal("123,456", value["a"]);
    }

    [Fact]
    public void TestNotPrimitiveKey()
    {
        var yaml = @"
? - item1
  - item2
: value
";
        var exception = Assert.Throws<DocfxException>(() => YamlUtility.Parse(new ErrorList(), yaml, null));
    }

    [Fact]
    public void TestAnchor()
    {
        var yaml = @"
a: &anchor test
b: *anchor
";
        Assert.Throws<DocfxException>(() => DeserializeWithValidation<Dictionary<string, string>>(yaml));
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
        Assert.Empty(errors.ToArray());
        Assert.NotNull(value);
        Assert.Equal(1, value.B);
        Assert.Equal("Good!", value.C);
        Assert.True(value.D);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    public void TestBoolean(string yaml, bool expected)
    {
        var (errors, actual) = DeserializeWithValidation<object>(yaml);
        Assert.Empty(errors.ToArray());
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("Null")]
    [InlineData("NULL")]
    public void TestNull(string yaml)
    {
        var (errors, _) = DeserializeWithValidation<object>(yaml);
        Assert.Empty(errors.ToArray());
    }

    [Theory]
    [InlineData("Infinity", "Infinity")]
    [InlineData("-Infinity", "-Infinity")]
    [InlineData("NaN", "NaN")]
    [InlineData(".inf", double.PositiveInfinity)]
    [InlineData("-.inf", double.NegativeInfinity)]
    [InlineData(".nan", double.NaN)]
    public void TestSpecialDouble(string yaml, object expected)
    {
        var (errors, value) = DeserializeWithValidation<object>(yaml);
        Assert.Empty(errors.ToArray());
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
        Assert.Empty(errors.ToArray());
        Assert.NotNull(values);
        Assert.Equal(10, values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            Assert.Equal(i, values[i].B);
            Assert.Equal($"Good{i}!", values[i].C);
            Assert.Equal(i % 2 != 0, values[i].D);
        }
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
        Assert.Empty(errors.ToArray().Where(error => error.Level == ErrorLevel.Error));
        Assert.NotNull(value);
        Assert.Equal(1, value.B);
        Assert.Equal("Good1!", value.C);
        Assert.True(value.D);
        Assert.Equal(1L, value.ValueDict["keyA"]);
        Assert.Equal("Good2!", value.ValueDict["keyB"]);
        Assert.True((bool)value.ValueDict["keyC"]);
        Assert.Equal("ItemA", value.ValueList[0]);
        Assert.Equal("True", value.ValueList[1]);
        Assert.Equal("3", value.ValueList[2]);
        Assert.Equal("ItemB", value.ValueList[3]);
        Assert.Equal(2L, value.ValueBasic.B);
        Assert.Equal("Good3!", value.ValueBasic.C);
        Assert.False(value.ValueBasic.D);
    }

    [Fact]
    public void TestStringEmpty()
    {
        var yaml = "";
        var (errors, _) = DeserializeWithValidation<ClassWithMoreMembers>(yaml);
        Assert.Empty(errors.ToArray());
    }

    [Fact]
    public void TestDuplicatedKeys()
    {
        var yaml = @"
Key1: 0
Key1: 1
";
        var errors = new ErrorList();
        var result = YamlUtility.Parse(errors, yaml, null);
        Assert.Collection(
            errors.ToArray(),
            e => Assert.Equal("Key 'Key1' is already defined, remove the duplicate key.", e.Message));
        Assert.Equal("1", result.Value<string>("Key1"));
    }

    [Theory]
    [InlineData("1", 1, 1)]
    [InlineData("name: name", 1, 7)]
    [InlineData(
        @"
items:
 - name: 1", 3, 2)]
    public void TestParsedJTokenHasLineInfo(string yaml, int expectedLine, int expectedColumn)
    {
        var errors = new ErrorList();
        var value = YamlUtility.Parse(errors, yaml, new FilePath("file"));
        Assert.Empty(errors.ToArray());

        // Get the first JValue of the first JProperty if any
        var source = JsonUtility.GetSourceInfo(value.Children().Any() ? value.Children().First().Children().First() : value);
        Assert.Equal(expectedLine, source.Line);
        Assert.Equal(expectedColumn, source.Column);
    }

    [Theory]
    [InlineData(@"b: not number")]
    public void ThrowWithoutSchemaValidation(string yaml)
    {
        Assert.ThrowsAny<Exception>(() => YamlUtility.DeserializeData<BasicClass>(yaml, null));
    }

    /// <summary>
    /// De-serialize a user input string to an object, return error list at the same time
    /// </summary>
    private static (ErrorList errors, T model) DeserializeWithValidation<T>(string json) where T : class, new()
    {
        var errors = new ErrorList();
        var token = YamlUtility.Parse(errors, json, null);
        var result = JsonUtility.ToObject<T>(errors, token);
        return (errors, result);
    }

    private class BasicClass
    {
        public int B { get; set; }

        public string C { get; set; }

        public bool D { get; set; }
    }

    private sealed class ClassWithMoreMembers : BasicClass
    {
        public Dictionary<string, object> ValueDict { get; set; }

        public List<string> ValueList { get; set; }

        public BasicClass ValueBasic { get; set; }
    }
}
