// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build;

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
        var result = JsonUtility.ReadMime(new StringReader(input), new FilePath(""));
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
        using var sw = new StringWriter();
        JsonUtility.Serialize(sw, new BasicClass { C = input });
        var json = sw.ToString();
        var (errors, value) = DeserializeWithValidation<BasicClass>(json);
        Assert.Empty(errors.ToArray());
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
        Assert.Empty(errors.ToArray());
        Assert.NotNull(value);
        Assert.Equal(1, value.B);
        Assert.Equal("Good!", value.C);
        Assert.True(value.D);
    }

    [Fact]
    public void TestJsonDeserializeCaseInsensitive()
    {
        var (_, value) = DeserializeWithValidation<BasicClass>("{\"B\":1}");
        Assert.Equal(1, value.B);
    }

    [Fact]
    public void TestBasicClassWithNullCharacter()
    {
        var json = JsonUtility.Serialize(new BasicClass { C = null, });
        Assert.Equal("{\"b\":0,\"d\":false}", json);
        var (errors, value) = DeserializeWithValidation<BasicClass>(json);
        Assert.Empty(errors.ToArray());
        Assert.NotNull(value);
        Assert.Equal(0, value.B);
        Assert.Null(value.C);
        Assert.False(value.D);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void TestBoolean(string json, bool expected)
    {
        var (errors, actual) = DeserializeWithValidation<object>(json);
        Assert.Empty(errors.ToArray());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestListOfBasicClass()
    {
        var json = JsonUtility.Serialize(
            (from i in Enumerable.Range(0, 10)
             select new BasicClass { B = i, C = $"Good{i}!", D = i % 2 == 0 }).ToList());
        var (errors, values) = DeserializeWithValidation<List<BasicClass>>(json);
        Assert.Empty(errors.ToArray());
        Assert.NotNull(values);
        Assert.Equal(10, values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            Assert.Equal(i, values[i].B);
            Assert.Equal($"Good{i}!", values[i].C);
            Assert.Equal(i % 2 == 0, values[i].D);
        }
    }

    [Fact]
    public void TestClassWithMoreMembersBySerializeThenDeserialize()
    {
        using var sw = new StringWriter();
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
                        { "a", true },
                },
                ValueList = new List<string> { "b", "a", },
                ValueBasic = new BasicClass
                {
                    D = false,
                    B = 5,
                    C = "Amazing!",
                },
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
  ""c"": ""Good!"",
  ""b"": 1,
  ""d"": true
}".Replace("\r\n", "\n"),
            json.Replace("\r\n", "\n"));
        var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
        Assert.Empty(errors.ToArray().Where(error => error.Level == ErrorLevel.Error));
        Assert.NotNull(value);
        Assert.Equal(1, value.B);
        Assert.Equal("Good!", value.C);
        Assert.True(value.D);
        Assert.Equal(5, value.ValueBasic.B);
        Assert.Equal("Amazing!", value.ValueBasic.C);
        Assert.False(value.ValueBasic.D);
        Assert.True((bool)value.ValueDict["a"]);
        Assert.Equal("valueA", value.ValueDict["b"]);
        Assert.Equal(10L, value.ValueDict["c"]);
        Assert.Equal("b", value.ValueList[0]);
        Assert.Equal("a", value.ValueList[1]);
    }

    [Fact]
    public void TestEmptyString()
    {
        var json = "";
        var exception = Assert.Throws<DocfxException>(() => JsonUtility.Parse(new ErrorList(), json, null));
    }

    [Theory]
    [InlineData("1", 1, 1)]
    [InlineData(@"{""key"":""value""}", 1, 14)]
    [InlineData(
        @"{
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
    [InlineData(
        @"{
""b"": 1,
""c"": ""c"",
""e"": ""e""}", typeof(NotSealedClass))]
    [InlineData(
        @"{
""data"":{
""b"": 1,
""c"": ""c"",
""e"": ""e""}}", typeof(SealedClassNestedWithNotSealedType))]
    [InlineData(
        @"{
""items"":[{
""b"": 1,
""c"": ""c"",
""e"": ""e""}]}", typeof(SealedClassNestedWithNotSealedType))]
    [InlineData(
        @"[{
""b"": 1,
""c"": ""c"",
""e"": ""e""}]", typeof(List<NotSealedClass>))]
    public void TestObjectTypeWithNotSealedType(string json, Type type)
    {
        var errors = new ErrorList();
        var token = JsonUtility.Parse(errors, json, null);
        _ = JsonUtility.ToObject(errors, token, type);
        Assert.Empty(errors.ToArray());
    }

    [Fact]
    public void TestMultipleSchemaViolationForPrimitiveType()
    {
        var json = @"{
""numberList"": [1, ""a""],
""b"" : ""b"",
""valueEnum"":""Four""}";
        var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
        Assert.Collection(
            errors.ToArray(),
            error =>
        {
            Assert.Equal(ErrorLevel.Error, error.Level);
            Assert.Equal("violate-schema", error.Code);
            Assert.Equal(2, error.Source.Line);
            Assert.Equal(21, error.Source.Column);
        },
            error =>
        {
            Assert.Equal(ErrorLevel.Error, error.Level);
            Assert.Equal("violate-schema", error.Code);
            Assert.Equal(3, error.Source.Line);
            Assert.Equal(9, error.Source.Column);
        },
            error =>
        {
            Assert.Equal(ErrorLevel.Error, error.Level);
            Assert.Equal("violate-schema", error.Code);
            Assert.Equal(4, error.Source.Line);
            Assert.Equal(18, error.Source.Column);
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
]
}";
        var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json);
        Assert.Collection(
            errors.ToArray(),
            error =>
        {
            Assert.Equal(ErrorLevel.Error, error.Level);
            Assert.Equal(4, error.Source.Line);
            Assert.Equal(27, error.Source.Column);
            Assert.Equal("Error converting value \"notArray\" to type 'System.Collections.Generic.List`1[Microsoft.Docs.Build.JsonUtilityTest+BasicClass]'.", error.Message);
        },
            error =>
        {
            Assert.Equal(ErrorLevel.Error, error.Level);
            Assert.Equal(5, error.Source.Line);
            Assert.Equal(22, error.Source.Column);
            Assert.Equal("Could not convert string to boolean: notBool.", error.Message);
        });
    }

    [Theory]
    [InlineData(@"{'b: 1 }")]
    [InlineData(@"{'b': 'not number'}")]
    public void SyntaxErrorShouldBeThrownWithoutSchemaValidation(string json)
    {
        var exception = Assert.Throws<DocfxException>(() => JsonUtility.DeserializeData<BasicClass>(json.Replace('\'', '\"'), null));
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
    [InlineData("{'a':1}", "{'A':2}", "{'a':1,'A':2}")]
    public void TestJsonMerge(string a, string b, string result)
    {
        var container = JObject.Parse(a.Replace('\'', '\"'));
        JsonUtility.Merge(Array.Empty<string>(), container, JObject.Parse(b.Replace('\'', '\"')));
        Assert.Equal(result.Replace('\'', '\"'), container.ToString(Formatting.None));
    }

    [Theory]
    [InlineData("{'a':[1]}", "{'a':[2]}", "{'a':[2]}", "b")]
    [InlineData("{'a':[1]}", "{'a':'2'}", "{'a':'2'}")]
    [InlineData("{'a':[1]}", "{'a':[2]}", "{'a':[2]}")]
    [InlineData("{'a':[1]}", "{'a':[2]}", "{'a':[2,1]}", "a")]
    public void TestJsonMergeWithUnionProperties(string a, string b, string result, params string[] unionProperties)
    {
        var container = JObject.Parse(a.Replace('\'', '\"'));
        JsonUtility.Merge(unionProperties, container, JObject.Parse(b.Replace('\'', '\"')));
        Assert.Equal(result.Replace('\'', '\"'), container.ToString(Formatting.None));
    }

    [Fact]
    public void NullValueHasSourceInfo()
    {
        var json = JsonUtility.Parse(new ErrorList(), "{'a': null,'b': null}".Replace('\'', '"'), new FilePath("file"));
        var obj = JsonUtility.ToObject<ClassWithSourceInfo>(new ErrorList(), json);

        Assert.NotNull(obj?.A);

        Assert.Null(obj.A.Value);
        Assert.Equal(1, obj.A.Source.Line);
        Assert.Equal(10, obj.A.Source.Column);
        Assert.Equal("file", obj.A.Source.File.Path);

        Assert.Equal("b", obj.B.Value);
        Assert.Equal(1, obj.B.Source.Line);
        Assert.Equal(20, obj.B.Source.Column);
        Assert.Equal("file", obj.B.Source.File.Path);
    }

    [Fact]
    public void JsonWithTypeErrorsHasCorrectSourceInfo()
    {
        var json = JsonUtility.Parse(
            new ErrorList(),
            "{'next': {'a': ['a1','a2'],'b': 'b'}, 'c': 'c'}".Replace('\'', '"'), new FilePath("file"));
        var obj = JsonUtility.ToObject<ClassWithSourceInfo>(new ErrorList(), json);

        Assert.Null(obj.Next.A.Value);
        Assert.Equal("b", obj.Next.B);
        Assert.Equal("c", obj.C);
    }

    [Fact]
    public void TestSerializeSourceInfoWithEmptyValue()
    {
        var basic = new BasicClass
        {
            B = 1,
            Property = new SourceInfo<string>(null, new SourceInfo(new FilePath(""))),
            Array = new SourceInfo<string[]>(Array.Empty<string>(), new SourceInfo(new FilePath(""))),
            GenericArray = new SourceInfo<List<string>>(new List<string>(), new SourceInfo(new FilePath(""))),
        };
        var result = JsonUtility.Serialize(basic);
        Assert.Equal("{\"b\":1,\"d\":false}", result);
    }

    [Theory]
    [InlineData("{}---")]
    [InlineData("{}{}")]
    public void TestDeserializeJsonWithCheckAdditionalContent(string json)
    {
        Assert.Throws<DocfxException>(() => JsonUtility.DeserializeData<ClassWithSourceInfo>(new StringReader(json), new FilePath("path"), true));
    }

    [Theory]
    [InlineData("{}---")]
    [InlineData("{}{}")]
    public void TestDeserializeJsonWithoutCheckAdditionalContent(string json)
    {
        var result = JsonUtility.DeserializeData<ClassWithSourceInfo>(new StringReader(json), new FilePath("path"), false);
        Assert.NotNull(result);
    }

    [Fact]
    public void TestDeserializeWithSourceInfo()
    {
        var result = JsonUtility.DeserializeData<ClassWithSourceInfo>("{\"a\": \"a value\"}", new FilePath("path"));
        Assert.NotNull(result.A.Source);
        Assert.Equal("path", result.A.Source.File.Path);
        Assert.Equal(1, result.A.Source.Line);
        Assert.Equal(15, result.A.Source.Column);
        Assert.Equal("a value", result.A.Value);
    }

    [Fact]
    public void TestExtensionDataWithSourceInfo()
    {
        var (_, result) = DeserializeWithValidation<ClassWithExtensionData>("{\"a\": 1} ");
        Assert.NotNull(result.ExtensionData["a"]);

        var valueSource = JsonUtility.GetSourceInfo(result.ExtensionData["a"]);
        Assert.NotNull(valueSource);
        Assert.Equal(1, valueSource.Line);
        Assert.Equal(7, valueSource.Column);

        var keySource = JsonUtility.GetKeySourceInfo(result.ExtensionData["a"]);
        Assert.NotNull(keySource);
        Assert.Equal(1, keySource.Line);
        Assert.Equal(5, keySource.Column);
    }

    [Theory]
    [InlineData(
        @"{
'numberList':
  [1, 'a']}", ErrorLevel.Error, "violate-schema", 3, 9)]
    [InlineData(@"{'b' : 'b'}", ErrorLevel.Error, "violate-schema", 1, 10)]
    [InlineData(@"{'valueEnum':'Four'}", ErrorLevel.Error, "violate-schema", 1, 19)]
    internal void TestMismatchingPrimitiveFieldType(
        string json, ErrorLevel expectedErrorLevel, string expectedErrorCode, int expectedErrorLine, int expectedErrorColumn)
    {
        var (errors, value) = DeserializeWithValidation<ClassWithMoreMembers>(json.Replace('\'', '\"'));
        Assert.Collection(errors.ToArray(), error =>
        {
            Assert.Equal(expectedErrorLevel, error.Level);
            Assert.Equal(expectedErrorCode, error.Code);
            Assert.Equal(expectedErrorLine, error.Source.Line);
            Assert.Equal(expectedErrorColumn, error.Source.Column);
        });
    }

    [Theory]
    [InlineData("{'name':'title','items':[,{'name':'1'}]}", "'items' contains null value, the null value has been removed.", "null-array-value", ErrorLevel.Warning)]
    [InlineData("[1,,1,1]", "'[1]' contains null value, the null value has been removed.", "null-array-value", ErrorLevel.Warning)]
    internal void TestNullValue(string json, string message, string errorCode, ErrorLevel errorLevel)
    {
        var errors = new ErrorList();
        var result = JsonUtility.Parse(errors, json.Replace('\'', '"'), null);
        Assert.Collection(errors.ToArray(), error =>
        {
            Assert.Equal(errorLevel, error.Level);
            Assert.Equal(errorCode, error.Code);
            Assert.Equal(message, error.Message);
        });
    }

    /// <summary>
    /// Deserialize from yaml string, return error list at the same time
    /// </summary>
    private static (ErrorList, T) DeserializeWithValidation<T>(string input) where T : class, new()
    {
        var errors = new ErrorList();
        var token = JsonUtility.Parse(errors, input, null);
        var result = JsonUtility.ToObject<T>(errors, token);
        return (errors, result);
    }

    internal class ClassWithSourceInfo
    {
        public SourceInfo<string> A { get; set; }

        public SourceInfo<string> B { get; set; } = new SourceInfo<string>("b");

        public ClassWithSourceInfo Next { get; set; }

        public string C { get; set; }
    }

    internal class EmptyEnumerable
    {
        public string A { get; set; } = "";

        public List<string> B { get; set; } = new List<string>();

        public IReadOnlyDictionary<string, object> C { get; set; } = new Dictionary<string, object>();

        public int[] D { get; set; } = Array.Empty<int>();

        public IReadOnlyList<string> E { get; set; } = new List<string>();

        public IReadOnlyCollection<object> F { get; set; } = new List<object>();

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Test")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Test")]
        public Dictionary<string, Dictionary<string, object>> G = new();
    }

    internal class BasicClass
    {
        public string C { get; set; }

        public int B { get; set; }

        public bool D { get; set; }

        public SourceInfo<string> Property { get; set; }

        public SourceInfo<List<string>> GenericArray { get; set; }

        public SourceInfo<string[]> Array { get; set; }
    }

    internal sealed class AnotherBasicClass
    {
        public int F { get; set; }

        public string G { get; set; }

        public bool H { get; set; }

        public List<BasicClass> Items { get; set; }
    }

    internal sealed class ClassWithMoreMembers : BasicClass
    {
        public Dictionary<string, object> ValueDict { get; set; }

        public List<string> ValueList { get; set; }

        public BasicClass ValueBasic { get; set; }

        public List<BasicClass> Items { get; set; }

        public List<AnotherBasicClass> AnotherItems { get; set; }

        public List<List<AnotherBasicClass>> NestedItems { get; set; }

        public List<int> NumberList { get; set; }

        public NestedClass NestedMember { get; set; }

        // make it nullable, so that json serializer would not make a default value
        public BasicEnum? ValueEnum { get; set; }
    }

    internal class NotSealedClass : BasicClass
    {
        public NestedClass NestedSealedMember { get; set; }
    }

    internal sealed class SealedClassNestedWithNotSealedType : BasicClass
    {
        public NotSealedClass Data { get; set; }

        public List<NotSealedClass> Items { get; set; }
    }

    internal sealed class NestedClass
    {
        public string ValueWithLengthRestriction { get; set; }
    }

    internal class ClassWithExtensionData
    {
        [JsonExtensionData]
        public JObject ExtensionData { get; set; } = new JObject();
    }

    internal enum BasicEnum
    {
        One,
        Two,
        Three,
    }
}
