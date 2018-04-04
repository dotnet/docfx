// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    using Xunit;
    using YamlDotNet.Core;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    [Trait("Owner", "zhyan")]
    public class YamlSerializationTest
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
            YamlUtility.Serialize(sw, new BasicClass { C = input });
            var yaml = sw.ToString();
            var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(input, value.C);
        }

        [Fact]
        public void TestNotWorkInYamlDotNet39()
        {
            const string Text = "😄";
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new BasicClass { C = Text });
            var yaml = sw.ToString();
            var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(Text, value.C);
        }

        [Fact]
        public void TestBasicClass()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new BasicClass { B = 1, C = "Good!" }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Equal(@"### YamlMime:Test-Yaml-Mime
B: 1
C: Good!
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            Assert.Equal("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
            var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
        }

        [Fact]
        public void TestBasicClassWithNullCharactor()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new BasicClass { B = 1, C = "~" }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Equal(@"### YamlMime:Test-Yaml-Mime
B: 1
C: ""~""
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            Assert.Equal("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
            var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("~", value.C);
        }

        [Fact]
        public void TestBoolean()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new object[] { true, false }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Equal(@"### YamlMime:Test-Yaml-Mime
- true
- false
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            Assert.Equal("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
            var value = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(2, value.Length);
            Assert.Equal(true, value[0]);
            Assert.Equal(false, value[1]);
            var value2 = YamlUtility.Deserialize<object[]>(new StringReader(@"### YamlMime:Test-Yaml-Mime
- true
- True
- TRUE
- false
- False
- FALSE
"));
            Assert.NotNull(value2);
            Assert.Equal(new[] { true, true, true, false, false, false }, value2.Cast<bool>());
        }

        [Fact]
        public void TestBigInteger()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new object[] { 1234567890000L, 9876543210000L, long.MaxValue, ulong.MaxValue }, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Equal(@"### YamlMime:Test-Yaml-Mime
- 1234567890000
- 9876543210000
- 9223372036854775807
- 18446744073709551615
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            Assert.Equal("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
            var value = YamlUtility.Deserialize<object[]>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(4, value.Length);
            Assert.Equal(1234567890000L, value[0]);
            Assert.Equal(9876543210000L, value[1]);
            Assert.Equal(long.MaxValue, value[2]);
            Assert.Equal(ulong.MaxValue, value[3]);
        }

        [Fact]
        public void TestYamlMime_Success()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, 1, YamlMime.YamlMimePrefix + "Test-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Equal("YamlMime:Test-Yaml-Mime", YamlMime.ReadMime(new StringReader(yaml)));
        }

        [Fact]
        public void TestYamlMime_NoYamlMime()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, 1, "No-Yaml-Mime");
            var yaml = sw.ToString();
            Assert.Null(YamlMime.ReadMime(new StringReader(yaml)));
        }

        [Fact]
        public void TestListOfBasicClass()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(
                sw,
                (from i in Enumerable.Range(0, 10)
                 select new BasicClass { B = i, C = $"Good{i}!" }).ToList());
            var yaml = sw.ToString();
            var values = YamlUtility.Deserialize<List<BasicClass>>(new StringReader(yaml));
            Assert.NotNull(values);
            Assert.Equal(10, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(i, values[i].B);
                Assert.Equal($"Good{i}!", values[i].C);
            }
        }

        public class BasicClass
        {
            public int B { get; set; }
            public string C { get; set; }
        }

        [Fact]
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
            Assert.Equal(@"B: 1
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
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.Equal(2, value.StringExtensions.Count);
            Assert.Equal("aaa", value.StringExtensions["a"]);
            Assert.Equal("bbb", value.StringExtensions["b"]);
            Assert.Equal(2, value.IntegerExtensions.Count);
            Assert.Equal(1, value.IntegerExtensions["x"]);
            Assert.Equal(2, value.IntegerExtensions["y"]);
            Assert.Equal(2, value.ObjectExtensions.Count);
            Assert.Equal(new[] { "foo1" }, (List<object>)value.ObjectExtensions["foo"]);
            Assert.Equal("bar", (string)value.ObjectExtensions["bar"]);
        }

        [Fact]
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
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
            Assert.Equal(2, value.StringExtensions.Count);
            Assert.Equal("aaa", value.StringExtensions["a"]);
            Assert.Equal("bbb", value.StringExtensions["b"]);
            Assert.Equal(2, value.IntegerExtensions.Count);
            Assert.Equal(1, value.IntegerExtensions["x"]);
            Assert.Equal(2, value.IntegerExtensions["y"]);
            Assert.Equal(2, value.ObjectExtensions.Count);
            Assert.Equal(new[] { "foo1" }, (List<object>)value.ObjectExtensions["foo"]);
            Assert.Equal("bar", (string)value.ObjectExtensions["bar"]);

            var sw = new StringWriter();
            YamlUtility.Serialize(sw, value);
            Assert.Equal(yaml, sw.ToString().Replace("\r\n", "\n"));
        }

        [Fact]
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
            Assert.NotNull(values);
            Assert.Equal(10, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(i, values[i].B);
                Assert.Equal($"Good{i}!", values[i].C);
                Assert.Equal(2, values[i].StringExtensions.Count);
                Assert.Equal($"aaa{i}", values[i].StringExtensions[$"a{i}"]);
                Assert.Equal($"bbb{i}", values[i].StringExtensions[$"b{i}"]);
                Assert.Equal(2, values[i].IntegerExtensions.Count);
                Assert.Equal(i + 1, values[i].IntegerExtensions[$"x{i}"]);
                Assert.Equal(i + 2, values[i].IntegerExtensions[$"y{i}"]);
                Assert.Equal(2, values[i].ObjectExtensions.Count);
                Assert.Equal(new[] { $"foo{i}" }, (List<object>)values[i].ObjectExtensions[$"foo{i}"]);
                Assert.Equal($"bar{i}", (string)values[i].ObjectExtensions[$"bar{i}"]);
            }
        }

        public class ClassWithExtensibleMembers : BasicClass
        {
            [ExtensibleMember("s.")]
            public SortedDictionary<string, string> StringExtensions { get; } = new SortedDictionary<string, string>();
            [ExtensibleMember("i.")]
            public SortedList<string, int> IntegerExtensions { get; } = new SortedList<string, int>();
            [ExtensibleMember()]
            public Dictionary<string, object> ObjectExtensions { get; } = new Dictionary<string, object>();
        }

        [Fact]
        public void TestInternalClass()
        {
            Assert.Throws<YamlException>(() => YamlUtility.Serialize(new StringWriter(), new InternalClass { }));
            Assert.Throws<YamlException>(() => YamlUtility.Deserialize<InternalClass>(new StringReader("A: a")));
        }

        internal class InternalClass
        {
            public string A { get; set; }
        }

        [Fact]
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

        [Fact]
        public void TestClassWithInterfaceMember()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new ClassWithInterfaceMember
            {
                List = new List<string> { "a" },
                ReadOnlyList = new[] { "b" },
                Collection = new Collection<string> { "c" },
                ReadOnlyCollection = ImmutableList.Create("d"),
                Enumerable = Enumerable.Range(1, 1),
                Dictionary = new Dictionary<string, string> { ["k1"] = "v1" },
                ReadOnlyDictionary = new SortedDictionary<string, string> { ["k2"] = "v2" },
                Set = new SortedSet<string> { "s" },
            });
            Assert.Equal(@"List:
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
            Assert.NotNull(obj);
            Assert.Single(obj.List);
            Assert.Equal("a", obj.List[0]);
            Assert.Single(obj.ReadOnlyList);
            Assert.Equal("b", obj.ReadOnlyList[0]);
            Assert.Single(obj.Collection);
            Assert.Equal("c", obj.Collection.First());
            Assert.Single(obj.ReadOnlyCollection);
            Assert.Equal("d", obj.ReadOnlyCollection.First());
            Assert.Single(obj.Enumerable);
            Assert.Equal(1, obj.Enumerable.First());
            Assert.Single(obj.Dictionary);
            Assert.Equal(new KeyValuePair<string, string>("k1", "v1"), obj.Dictionary.First());
            Assert.Single(obj.ReadOnlyDictionary);
            Assert.Equal(new KeyValuePair<string, string>("k2", "v2"), obj.ReadOnlyDictionary.First());
            Assert.Single(obj.Set);
            Assert.Equal("s", obj.Set.First());
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
}
