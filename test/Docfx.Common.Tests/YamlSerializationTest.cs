// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Docfx.YamlSerialization;
using YamlDotNet.Core;

namespace Docfx.Common.Tests;

[TestClass]
public class YamlSerializationTest
{
    [TestMethod]
    [DataRow(" Add --globalMetadata, --globalMetadataFile and --fileMetadataFile\n")]
    [DataRow("\r\n Hello\n")]
    [DataRow("  \r\n Hello\n")]
    [DataRow("True")]
    [DataRow("true")]
    [DataRow("TRUE")]
    [DataRow("False")]
    [DataRow("false")]
    [DataRow("FALSE")]
    [DataRow("Null")]
    [DataRow("null")]
    [DataRow("NULL")]
    public void TestObjectWithStringProperty(string input)
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new BasicClass { C = input });
        var yaml = sw.ToString();
        var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(input, value.C);
    }

    [TestMethod]
    [DataRow("123")]
    [DataRow("1.23")]
    [DataRow("0.123")]
    [DataRow(".123")]
    [DataRow("0.")]
    [DataRow("-0.0")]
    [DataRow(".5")]
    [DataRow("+12e03")]
    [DataRow("-2E+05")]
    public void TestScalarLikeStringValueNeedDoubleQuoted(string input)
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, input);
        var yaml = sw.ToString();
        Assert.AreEqual($"\"{input}\"", yaml.Trim());
    }

    [TestMethod]
    public void TestNotWorkInYamlDotNet39()
    {
        const string Text = "😄";
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new BasicClass { C = Text });
        var yaml = sw.ToString();
        var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(Text, value.C);
    }

    [TestMethod]
    public void TestBasicClass()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new BasicClass { B = 1, C = "Good!" }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.AreEqual(@"### YamlMime:Test-Yaml-Mime
B: 1
C: Good!
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        Assert.AreEqual("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
        var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(1, value.B);
        Assert.AreEqual("Good!", value.C);
    }

    [TestMethod]
    public void TestBasicClassWithNullCharacter()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new BasicClass { B = 1, C = "~" }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.AreEqual(@"### YamlMime:Test-Yaml-Mime
B: 1
C: ""~""
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        Assert.AreEqual("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
        var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(1, value.B);
        Assert.AreEqual("~", value.C);
    }

    [TestMethod]
    public void TestBoolean()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new object[] { true, false }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.AreEqual(@"### YamlMime:Test-Yaml-Mime
- true
- false
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        Assert.AreEqual("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
        var value = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(2, value.Length);
        Assert.AreEqual(true, value[0]);
        Assert.AreEqual(false, value[1]);
        var value2 = YamlUtility.Deserialize<object[]>(new StringReader(@"### YamlMime:Test-Yaml-Mime
- true
- True
- TRUE
- false
- False
- FALSE
"));
        Assert.IsNotNull(value2);
        Assert.AreEqual(new[] { true, true, true, false, false, false }, value2.Cast<bool>());
    }

    [TestMethod]
    public void TestBigInteger()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new object[] { 1234567890000L, 9876543210000L, long.MaxValue, ulong.MaxValue }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.AreEqual(@"### YamlMime:Test-Yaml-Mime
- 1234567890000
- 9876543210000
- 9223372036854775807
- 18446744073709551615
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        Assert.AreEqual("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
        var value = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(4, value.Length);
        Assert.AreEqual(1234567890000L, value[0]);
        Assert.AreEqual(9876543210000L, value[1]);
        Assert.AreEqual(long.MaxValue, value[2]);
        Assert.AreEqual(ulong.MaxValue, value[3]);
    }

    [TestMethod]
    public void TestYamlMime_Success()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, 1, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.AreEqual("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
    }

    [TestMethod]
    public void TestYamlMime_NoYamlMime()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, 1, "No-Yaml-Mime");
        var yaml = sw.ToString();
        Assert.IsNull(YamlMime.ReadMime(new StringReader(yaml)));
    }

    [TestMethod]
    public void TestListOfBasicClass()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(
            sw,
            (from i in Enumerable.Range(0, 10)
             select new BasicClass { B = i, C = $"Good{i}!" }).ToList());
        var yaml = sw.ToString();
        var values = YamlUtility.Deserialize<List<BasicClass>>(new StringReader(yaml));
        Assert.IsNotNull(values);
        Assert.AreEqual(10, values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            Assert.AreEqual(i, values[i].B);
            Assert.AreEqual($"Good{i}!", values[i].C);
        }
    }

    public class BasicClass
    {
        public int B { get; set; }
        public string C { get; set; }
    }

    [TestMethod]
    public void TestClassWithExtensibleMembersBySerializeThenDeserialize()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(
            sw,
            new ClassWithExtensibleMembers
            {
                B = 1,
                C = "Good!",
                StringExtensions =
                {
                    ["a"] = "aaa",
                    ["b"] = "bbb",
                },
                IntegerExtensions =
                {
                    ["x"] = 1,
                    ["y"] = 2,
                },
                ObjectExtensions =
                {
                    ["foo"] = new List<string> { "foo1" },
                    ["bar"] = "bar",
                }
            });
        var yaml = sw.ToString();
        Assert.AreEqual(@"B: 1
C: Good!
s.a: aaa
s.b: bbb
i.x: 1
i.y: 2
foo:
- foo1
bar: bar
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
        var value = YamlUtility.Deserialize<ClassWithExtensibleMembers>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(1, value.B);
        Assert.AreEqual("Good!", value.C);
        Assert.AreEqual(2, value.StringExtensions.Count);
        Assert.AreEqual("aaa", value.StringExtensions["a"]);
        Assert.AreEqual("bbb", value.StringExtensions["b"]);
        Assert.AreEqual(2, value.IntegerExtensions.Count);
        Assert.AreEqual(1, value.IntegerExtensions["x"]);
        Assert.AreEqual(2, value.IntegerExtensions["y"]);
        Assert.AreEqual(2, value.ObjectExtensions.Count);
        Assert.AreEqual(new[] { "foo1" }, ((List<object>)value.ObjectExtensions["foo"]).ToArray());
        Assert.AreEqual("bar", (string)value.ObjectExtensions["bar"]);
    }

    [TestMethod]
    public void TestClassWithExtensibleMembersByDeserializeThenSerialize()
    {
        var yaml = @"B: 1
C: Good!
s.a: aaa
s.b: bbb
i.x: 1
i.y: 2
foo:
- foo1
bar: bar
".Replace("\r\n", "\n");
        var value = YamlUtility.Deserialize<ClassWithExtensibleMembers>(new StringReader(yaml));
        Assert.IsNotNull(value);
        Assert.AreEqual(1, value.B);
        Assert.AreEqual("Good!", value.C);
        Assert.AreEqual(2, value.StringExtensions.Count);
        Assert.AreEqual("aaa", value.StringExtensions["a"]);
        Assert.AreEqual("bbb", value.StringExtensions["b"]);
        Assert.AreEqual(2, value.IntegerExtensions.Count);
        Assert.AreEqual(1, value.IntegerExtensions["x"]);
        Assert.AreEqual(2, value.IntegerExtensions["y"]);
        Assert.AreEqual(2, value.ObjectExtensions.Count);
        Assert.AreEqual(new[] { "foo1" }, ((List<object>)value.ObjectExtensions["foo"]).ToArray());
        Assert.AreEqual("bar", (string)value.ObjectExtensions["bar"]);

        var sw = new StringWriter();
        YamlUtility.Serialize(sw, value);
        Assert.AreEqual(yaml, sw.ToString().Replace("\r\n", "\n"));
    }

    [TestMethod]
    public void TestListOfClassWithExtensibleMembers()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(
            sw,
            (from i in Enumerable.Range(0, 10)
             select new ClassWithExtensibleMembers
             {
                 B = i,
                 C = $"Good{i}!",
                 StringExtensions =
                {
                    [$"a{i}"] = $"aaa{i}",
                    [$"b{i}"] = $"bbb{i}",
                },
                 IntegerExtensions =
                {
                    [$"x{i}"] = i + 1,
                    [$"y{i}"] = i + 2,
                },
                 ObjectExtensions =
                {
                    [$"foo{i}"] = new List<string> { $"foo{i}" },
                    [$"bar{i}"] = $"bar{i}",
                }
             }).ToList());
        var yaml = sw.ToString();
        var values = YamlUtility.Deserialize<List<ClassWithExtensibleMembers>>(new StringReader(yaml));
        Assert.IsNotNull(values);
        Assert.AreEqual(10, values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            Assert.AreEqual(i, values[i].B);
            Assert.AreEqual($"Good{i}!", values[i].C);
            Assert.AreEqual(2, values[i].StringExtensions.Count);
            Assert.AreEqual($"aaa{i}", values[i].StringExtensions[$"a{i}"]);
            Assert.AreEqual($"bbb{i}", values[i].StringExtensions[$"b{i}"]);
            Assert.AreEqual(2, values[i].IntegerExtensions.Count);
            Assert.AreEqual(i + 1, values[i].IntegerExtensions[$"x{i}"]);
            Assert.AreEqual(i + 2, values[i].IntegerExtensions[$"y{i}"]);
            Assert.AreEqual(2, values[i].ObjectExtensions.Count);
            Assert.AreEqual(new[] { $"foo{i}" }, ((List<object>)values[i].ObjectExtensions[$"foo{i}"]).ToArray());
            Assert.AreEqual($"bar{i}", (string)values[i].ObjectExtensions[$"bar{i}"]);
        }
    }

    public class ClassWithExtensibleMembers : BasicClass
    {
        [ExtensibleMember("s.")]
        public SortedDictionary<string, string> StringExtensions { get; } = [];
        [ExtensibleMember("i.")]
        public SortedList<string, int> IntegerExtensions { get; } = [];
        [ExtensibleMember()]
        public Dictionary<string, object> ObjectExtensions { get; } = [];
    }

    [TestMethod]
    public void TestInternalClass()
    {
        YamlUtility.Serialize(new StringWriter(), new InternalClass { });
        YamlUtility.Deserialize<InternalClass>(new StringReader("A: a"));
    }

    internal class InternalClass
    {
        public string A { get; set; }
    }

    [TestMethod]
    public void TestClassWithInvalidExtensibleMember()
    {
        Assert.Throws<YamlException>(() => YamlUtility.Serialize(new StringWriter(), new ClassWithInvalidExtensibleMember { }));
        Assert.Throws<YamlException>(() => YamlUtility.Deserialize<ClassWithInvalidExtensibleMember>(new StringReader("A: a")));
    }

    public class ClassWithInvalidExtensibleMember
    {
        public string A { get; set; }
        [ExtensibleMember]
        public string StringExtensions { get; set; }
    }

    [TestMethod]
    public void TestClassWithInterfaceMember()
    {
        var sw = new StringWriter();
        YamlUtility.Serialize(sw, new ClassWithInterfaceMember
        {
            List = ["a"],
            ReadOnlyList = new[] { "b" },
            Collection = new Collection<string> { "c" },
            ReadOnlyCollection = ImmutableList.Create("d"),
            Enumerable = Enumerable.Range(1, 1),
            Dictionary = new Dictionary<string, string> { ["k1"] = "v1" },
            ReadOnlyDictionary = new SortedDictionary<string, string> { ["k2"] = "v2" },
            Set = new SortedSet<string> { "s" },
        });
        Assert.AreEqual(@"List:
- a
ReadOnlyList:
- b
Collection:
- c
ReadOnlyCollection:
- d
Enumerable:
- 1
Dictionary:
  k1: v1
ReadOnlyDictionary:
  k2: v2
Set:
- s
".Replace("\r\n", "\n"), sw.ToString().Replace("\r\n", "\n"));

        var obj = YamlUtility.Deserialize<ClassWithInterfaceMember>(new StringReader(sw.ToString()));
        Assert.IsNotNull(obj);
        Assert.ContainsSingle(obj.List);
        Assert.AreEqual("a", obj.List[0]);
        Assert.ContainsSingle(obj.ReadOnlyList);
        Assert.AreEqual("b", obj.ReadOnlyList[0]);
        Assert.ContainsSingle(obj.Collection);
        Assert.AreEqual("c", obj.Collection.First());
        Assert.ContainsSingle(obj.ReadOnlyCollection);
        Assert.AreEqual("d", obj.ReadOnlyCollection.First());
        Assert.ContainsSingle(obj.Enumerable);
        Assert.AreEqual(1, obj.Enumerable.First());
        Assert.ContainsSingle(obj.Dictionary);
        Assert.AreEqual(new KeyValuePair<string, string>("k1", "v1"), obj.Dictionary.First());
        Assert.ContainsSingle(obj.ReadOnlyDictionary);
        Assert.AreEqual(new KeyValuePair<string, string>("k2", "v2"), obj.ReadOnlyDictionary.First());
        Assert.ContainsSingle(obj.Set);
        Assert.AreEqual("s", obj.Set.First());
    }

    public class ClassWithInterfaceMember
    {
        public IList<string> List { get; set; }
        public IReadOnlyList<string> ReadOnlyList { get; set; }
        public ICollection<string> Collection { get; set; }
        public IReadOnlyCollection<string> ReadOnlyCollection { get; set; }
        public IEnumerable<int> Enumerable { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public IReadOnlyDictionary<string, string> ReadOnlyDictionary { get; set; }
        public ISet<string> Set { get; set; }
    }
}
