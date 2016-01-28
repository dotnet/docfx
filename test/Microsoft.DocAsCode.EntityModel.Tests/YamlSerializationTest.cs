// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;
    using YamlDotNet.Core;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    [Trait("Owner", "zhyan")]
    public class YamlSerializationTest
    {
        [Fact]
        public void TestBasicClass()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new BasicClass { B = 1, C = "Good!" });
            var yaml = sw.ToString();
            Assert.Equal(@"B: 1
C: Good!
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            var value = YamlUtility.Deserialize<BasicClass>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
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
        public void TestClassWithExtensibleMembers()
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
    }
}
