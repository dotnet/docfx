// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "CompositeDictionary")]
    public class CompositeDictionaryTest
    {
        [Fact]
        public void TestAddRemoveGetSetForCompositeDictionary()
        {
            var c = new C
            {
                D1 =
                {
                    ["a"] = 1.0,
                },
                D2 =
                {
                    ["b"] = 1,
                },
                D3 =
                {
                    ["c"] = "x",
                },
            };

            Assert.Equal(3, c.CD.Count);
            var list = c.CD.ToList();
            Assert.Equal("D1.a", list[0].Key);
            Assert.Equal(1.0, list[0].Value);
            Assert.Equal("D2.b", list[1].Key);
            Assert.Equal(1, list[1].Value);
            Assert.Equal("D3.c", list[2].Key);
            Assert.Equal("x", list[2].Value);

            Assert.Equal<string>(new[] { "D1.a", "D2.b", "D3.c" }, c.CD.Keys);
            Assert.Equal<object>(new object[] { 1.0, 1, "x" }, c.CD.Values);
            Assert.True(c.CD.ContainsKey("D1.a"));
            Assert.False(c.CD.ContainsKey("D1.b"));
            Assert.False(c.CD.ContainsKey("a"));

            c.CD.Add("D1.b", 2);
            Assert.Equal(4, c.CD.Count);
            Assert.Equal(2, c.D1.Count);
            Assert.True(c.CD.ContainsKey("D1.b"));
            Assert.Equal(2, c.CD["D1.b"]);
            Assert.True(c.CD.TryGetValue("D1.b", out object value));
            Assert.Equal(2, value);
            Assert.True(c.CD.TryGetValue("D2.b", out value));
            Assert.Equal(1, value);

            Assert.True(c.CD.Remove("D1.b"));
            Assert.False(c.CD.Remove("D1.b"));
            Assert.False(c.CD.Remove("x"));
            Assert.False(c.CD.TryGetValue("D1.b", out value));
            Assert.Null(value);
            Assert.False(c.CD.TryGetValue("b", out value));
            Assert.Null(value);
            Assert.Equal(3, c.CD.Count);
            Assert.Equal(1, c.D1.Count);

            Assert.Equal("x", c.CD["D3.c"]);
            c.CD["D3.c"] = "y";
            Assert.Equal("y", c.CD["D3.c"]);
            Assert.Equal(3, c.CD.Count);

            c.CD.Clear();
            Assert.Equal(0, c.CD.Count);
        }

        [Fact]
        public void TestThrowCaseForCompositeDictionary()
        {
            var c = new C
            {
                D1 =
                {
                    ["a"] = 1.0,
                },
                D2 =
                {
                    ["b"] = 1,
                },
                D3 =
                {
                    ["c"] = "x",
                },
            };

            Assert.Throws<InvalidOperationException>(() => c.CD["z"] = 1);
            Assert.Throws<InvalidOperationException>(() => c.CD.Add("z", 1));

            Assert.Throws<KeyNotFoundException>(() => c.CD["z"]);

            Assert.Throws<ArgumentNullException>(() => c.CD[null]);
            Assert.Throws<ArgumentNullException>(() => c.CD[null] = 1);
            Assert.Throws<ArgumentNullException>(() => c.CD.Add(null, 1));
            Assert.Throws<ArgumentNullException>(() => c.CD.Remove(null));

            Assert.Throws<InvalidCastException>(() => c.CD["D2.z"] = "a");
        }

        private sealed class C
        {
            public Dictionary<string, object> D1 { get; } = new Dictionary<string, object>();
            public SortedDictionary<string, int> D2 { get; } = new SortedDictionary<string, int>();
            public SortedList<string, string> D3 { get; } = new SortedList<string, string>();
            private CompositeDictionary _cd;
            public CompositeDictionary CD
            {
                get
                {
                    return _cd ??
                        (_cd = CompositeDictionary
                            .CreateBuilder()
                            .Add("D1.", D1)
                            .Add("D2.", D2)
                            .Add("D3.", D3)
                            .Create());
                }
            }
        }
    }
}
