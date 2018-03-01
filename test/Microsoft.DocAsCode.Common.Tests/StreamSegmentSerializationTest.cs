// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common.StreamSegmentSerialization;

    using Xunit;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "StreamSegmentSerialization")]
    public class StreamSegmentSerializationTest
    {
        [Fact]
        public void TestNull()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            ss.WriteNull();
            Assert.Equal(9, ms.Length);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            Assert.Null(sd.Read());
        }

        [Fact]
        public void TestInteger()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            ss.Write(101);
            Assert.Equal(13, ms.Length);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            Assert.Equal(101, sd.Read());
        }

        [Fact]
        public void TestString()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            ss.Write("Hello world!");
            Assert.Equal(22, ms.Length);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            Assert.Equal("Hello world!", sd.Read());
        }

        [Fact]
        public void TestArray()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            var expected = new object[] { 123, "abc", null };
            ss.Write(expected);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            Assert.Equal(expected, sd.Read());
        }

        [Fact]
        public void TestDictionary()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            var expected = new Dictionary<string, object>
            {
                ["a"] = 1,
                ["bcd"] = "efg",
                ["hijk"] = null,
            };
            ss.Write(expected);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            var actual = sd.Read();
            Assert.IsType<Dictionary<string, object>>(actual);
            var actualDict = (Dictionary<string, object>)actual;
            Assert.Equal(expected["a"], actualDict["a"]);
            Assert.Equal(expected["bcd"], actualDict["bcd"]);
            Assert.Equal(expected["hijk"], actualDict["hijk"]);
        }

        [Fact]
        public void TestDictionaryLazy()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            var expected = new Dictionary<string, object>
            {
                ["a"] = 1,
                ["bcd"] = "efg",
                ["hijk"] = null,
            };
            ss.Write(expected);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            var actual = sd.ReadDictionaryLazy(sd.ReadSegment());
            Assert.Equal(expected["a"], actual["a"].Value);
            Assert.Equal(expected["bcd"], actual["bcd"].Value);
            Assert.Equal(expected["hijk"], actual["hijk"].Value);
        }

        [Fact]
        public void TestSequence()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            ss.WriteNull();
            ss.Write("abc");
            ss.Write(123);
            ss.Write(new object[] { 1, 2, 3 });
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            Assert.Null(sd.Read());
            Assert.Equal("abc", sd.Read());
            Assert.Equal(123, sd.Read());
            Assert.Equal(new object[] { 1, 2, 3 }, sd.Read());
        }

        [Fact]
        public void TestNest()
        {
            var ms = new MemoryStream();
            var ss = new StreamSerializer(ms);
            var expected = new Dictionary<string, object>
            {
                ["a"] = new object[]
                {
                    "a1",
                    "a2",
                    new Dictionary<string, object>
                    {
                        ["b"] = new object[]
                        {
                            "b1",
                            "b2",
                        }
                    },
                },
            };
            ss.Write(expected);
            ms.Position = 0;
            var sd = new StreamDeserializer(ms);
            var actual = (Dictionary<string, object>)sd.Read();
            var a = (object[])actual["a"];
            Assert.Equal("a1", a[0]);
            Assert.Equal("a2", a[1]);
            var a2 = (Dictionary<string, object>)((object[])actual["a"])[2];
            var b = (object[])(a2["b"]);
            Assert.Equal("b1", b[0]);
            Assert.Equal("b2", b[1]);
        }
    }
}
