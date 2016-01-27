// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.YamlSerialization;


    [Trait("Owner", "zhyan")]
    public class YamlSerializationTest
    {
        [Fact]
        public void TestBasicObject()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(sw, new BasicObject { B = 1, C = "Good!" });
            var yaml = sw.ToString();
            Assert.Equal(@"B: 1
C: Good!
".Replace("\r\n", "\n"), yaml.Replace("\r\n", "\n"));
            var value = YamlUtility.Deserialize<BasicObject>(new StringReader(yaml));
            Assert.NotNull(value);
            Assert.Equal(1, value.B);
            Assert.Equal("Good!", value.C);
        }

        public class BasicObject
        {
            public int B { get; set; }
            public string C { get; set; }
        }

        [Fact]
        public void TestExtensibleObject()
        {
            var sw = new StringWriter();
            YamlUtility.Serialize(
                sw,
                new ExtensibleObject
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
            var value = YamlUtility.Deserialize<ExtensibleObject>(new StringReader(yaml));
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

        public class ExtensibleObject : BasicObject
        {
            [ExtensibleMember("s.")]
            public SortedDictionary<string, string> StringExtensions { get; } = new SortedDictionary<string, string>();
            [ExtensibleMember("i.")]
            public SortedList<string, int> IntegerExtensions { get; } = new SortedList<string, int>();
            [ExtensibleMember()]
            public Dictionary<string, object> ObjectExtensions { get; } = new Dictionary<string, object>();
        }

    }
}
