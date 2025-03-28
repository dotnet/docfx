// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.Tests;

[TestProperty("Related", "CompositeDictionary")]
[TestClass]
public class CompositeDictionaryTest
{
    [TestMethod]
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

        Assert.AreEqual(3, c.CD.Count);
        var list = c.CD.ToList();
        Assert.AreEqual("D1.a", list[0].Key);
        Assert.AreEqual(1.0, list[0].Value);
        Assert.AreEqual("D2.b", list[1].Key);
        Assert.AreEqual(1, list[1].Value);
        Assert.AreEqual("D3.c", list[2].Key);
        Assert.AreEqual("x", list[2].Value);

        CollectionAssert.AreEqual(new[] { "D1.a", "D2.b", "D3.c" }, c.CD.Keys.ToArray());
        CollectionAssert.AreEqual(new object[] { 1.0, 1, "x" }, c.CD.Values.ToArray());
        Assert.IsTrue(c.CD.ContainsKey("D1.a"));
        Assert.IsFalse(c.CD.ContainsKey("D1.b"));
        Assert.IsFalse(c.CD.ContainsKey("a"));

        c.CD.Add("D1.b", 2);
        Assert.AreEqual(4, c.CD.Count);
        Assert.AreEqual(2, c.D1.Count);
        Assert.IsTrue(c.CD.ContainsKey("D1.b"));
        Assert.AreEqual(2, c.CD["D1.b"]);
        Assert.IsTrue(c.CD.TryGetValue("D1.b", out object value));
        Assert.AreEqual(2, value);
        Assert.IsTrue(c.CD.TryGetValue("D2.b", out value));
        Assert.AreEqual(1, value);

        Assert.IsTrue(c.CD.Remove("D1.b"));
        Assert.IsFalse(c.CD.Remove("D1.b"));
        Assert.IsFalse(c.CD.Remove("x"));
        Assert.IsFalse(c.CD.TryGetValue("D1.b", out value));
        Assert.IsNull(value);
        Assert.IsFalse(c.CD.TryGetValue("b", out value));
        Assert.IsNull(value);
        Assert.AreEqual(3, c.CD.Count);
        Assert.ContainsSingle(c.D1);

        Assert.AreEqual("x", c.CD["D3.c"]);
        c.CD["D3.c"] = "y";
        Assert.AreEqual("y", c.CD["D3.c"]);
        Assert.AreEqual(3, c.CD.Count);

        c.CD.Clear();
        Assert.IsEmpty(c.CD);
    }

    [TestMethod]
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

        Assert.Throws<KeyNotFoundException>(() => _ = c.CD["z"]);

        Assert.Throws<ArgumentNullException>(() => _ = c.CD[null]);
        Assert.Throws<ArgumentNullException>(() => c.CD[null] = 1);
        Assert.Throws<ArgumentNullException>(() => c.CD.Add(null, 1));
        Assert.Throws<ArgumentNullException>(() => c.CD.Remove(null));

        Assert.Throws<InvalidCastException>(() => c.CD["D2.z"] = "a");
    }

    private sealed class C
    {
        public Dictionary<string, object> D1 { get; } = [];
        public SortedDictionary<string, int> D2 { get; } = [];
        public SortedList<string, string> D3 { get; } = [];
        private CompositeDictionary _cd;
        public CompositeDictionary CD
        {
            get
            {
                return _cd ??= CompositeDictionary
                        .CreateBuilder()
                        .Add("D1.", D1)
                        .Add("D2.", D2)
                        .Add("D3.", D3)
                        .Create();
            }
        }
    }
}
