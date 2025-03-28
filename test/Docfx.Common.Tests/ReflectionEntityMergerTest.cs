// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;

namespace Docfx.Common.Tests;

[TestProperty("Related", "ReflectionEntityMerger")]
[TestClass]
public class ReflectionEntityMergerTest
{
    [TestMethod]
    public void TestReflectionEntityMergerWithBasicScenarios()
    {
        var sample = new BasicSample
        {
            IntValue = 1,
            NullableIntValue = 2,
            Text = "abc",
            Nested = new BasicSample
            {
                IntValue = 1,
                NullableIntValue = 2,
                Text = null,
            }
        };
        var overrides = new BasicSample
        {
            IntValue = 10,
            Nested = new BasicSample
            {
                NullableIntValue = 22,
                Text = "Wow!",
                Nested = new BasicSample(),
            }
        };
        new MergerFacade(
            new ReflectionEntityMerger())
            .Merge(ref sample, overrides);
        Assert.AreEqual(10, sample.IntValue);
        Assert.AreEqual(2, sample.NullableIntValue);
        Assert.AreEqual("abc", sample.Text);
        Assert.AreEqual(1, sample.Nested.IntValue);
        Assert.AreEqual(22, sample.Nested.NullableIntValue);
        Assert.AreEqual("Wow!", sample.Nested.Text);
        Assert.AreSame(overrides.Nested.Nested, sample.Nested.Nested);
    }

    public class BasicSample
    {
        public int IntValue { get; set; }
        public int? NullableIntValue { get; set; }
        public string Text { get; set; }
        public BasicSample Nested { get; set; }
    }

    [TestMethod]
    public void TestReflectionEntityMergerWhenMergeNullOrDefault()
    {
        var sample = new MergeOptionSample
        {
            IntValue = 1,
            NullableIntValue = 2,
            Text = "abc",
            Array2 = ["a"],
            Nested = new MergeOptionSample
            {
                IntValue = 1,
                NullableIntValue = 2,
                Text = null,
                Nested = new MergeOptionSample(),
            }
        };
        var overrides = new MergeOptionSample
        {
            IntValue = 10,
            Array1 = [2],
            Nested = new MergeOptionSample
            {
                NullableIntValue = 22,
                Text = "Wow!",
            }
        };
        new MergerFacade(
            new KeyedListMerger(
                new ReflectionEntityMerger()))
            .Merge(ref sample, overrides);
        Assert.AreEqual(10, sample.IntValue);
        Assert.AreEqual(2, sample.NullableIntValue);
        Assert.IsNull(sample.Text);
        Assert.AreSame(overrides.Array1, sample.Array1);
        Assert.IsNull(sample.Array2);
        Assert.AreEqual(0, sample.Nested.IntValue);
        Assert.AreEqual(2, sample.Nested.NullableIntValue);
        Assert.AreEqual("Wow!", sample.Nested.Text);
        Assert.IsNull(sample.Nested.Nested);
    }

    public class MergeOptionSample
    {
        [MergeOption(MergeOption.MergeNullOrDefault)]
        public int IntValue { get; set; }
        [MergeOption(MergeOption.Ignore)]
        public int? NullableIntValue { get; set; }
        [MergeOption(MergeOption.MergeNullOrDefault)]
        public string Text { get; set; }
        [MergeOption(MergeOption.Replace)]
        public int[] Array1 { get; set; }
        [MergeOption(MergeOption.ReplaceNullOrDefault)]
        public string[] Array2 { get; set; }
        [MergeOption(MergeOption.MergeNullOrDefault)]
        public MergeOptionSample Nested { get; set; }
    }

    [TestMethod]
    public void TestReflectionEntityMergerWhenMergeListWithKey()
    {
        var sample = new List<ListItemSample>
        {
            new() { Key1 = "qwe", Key2 = 1, Text = "O1" },
            new() { Key1 = "asd", Key2 = 1, Text = "O2" },
            new() { Key1 = "asd", Key2 = 2, Text = "O3" },
        };
        var overrides = new List<ListItemSample>
        {
            new() { Key1 = "___", Key2 = 1, Text = "N1" },
            new() { Key1 = "asd", Key2 = 1, Text = "N2" },
            new() { Key1 = "asd", Key2 = 2, Text = "N3" },
        };
        new MergerFacade(
            new KeyedListMerger(
                new ReflectionEntityMerger()))
            .Merge(
                ref sample,
                overrides,
                new Dictionary<string, object> { { "separator", "->" }, });
        Assert.AreEqual("O1", sample[0].Text);
        Assert.AreEqual("O2->N2", sample[1].Text);
        Assert.AreEqual("O3->N3", sample[2].Text);
    }

    public class ListItemSample
    {
        [MergeOption(MergeOption.MergeKey)]
        public string Key1 { get; set; }
        [MergeOption(MergeOption.MergeKey)]
        public int Key2 { get; set; }
        [MergeOption(typeof(StringMergeHandler))]
        public string Text { get; set; }
    }

    public class StringMergeHandler : IMergeHandler
    {
        public void Merge(ref object source, object overrides, IMergeContext context)
        {
            var s = (string)source;
            var o = (string)overrides;
            source = s + context["separator"] + o;
        }
    }

    [TestMethod]
    public void TestMergeDictionary()
    {
        var sample = new Dictionary<string, BasicSample>
        {
            ["a"] = new BasicSample
            {
                IntValue = 1,
                NullableIntValue = 2,
                Text = "abc",
                Nested = new BasicSample
                {
                    IntValue = 1,
                    NullableIntValue = 2,
                    Text = null,
                }
            },
            ["b"] = new BasicSample
            {
                IntValue = 101,
                NullableIntValue = null,
                Text = "xyz",
                Nested = new BasicSample
                {
                    IntValue = 102,
                    NullableIntValue = 2,
                    Text = null,
                }
            },
        };
        var overrides = new Dictionary<string, BasicSample>
        {
            ["a"] = new BasicSample
            {
                IntValue = 10,
                Nested = new BasicSample
                {
                    NullableIntValue = 22,
                    Text = "Wow!",
                    Nested = new BasicSample(),
                }
            },
            ["c"] = new BasicSample
            {
                IntValue = 10,
                Nested = new BasicSample
                {
                    NullableIntValue = 22,
                    Text = "Wow!",
                    Nested = new BasicSample(),
                }
            }
        };
        new MergerFacade(
            new DictionaryMerger(
                new ReflectionEntityMerger()))
            .Merge(
                ref sample,
                overrides);

        Assert.AreEqual(10, sample["a"].IntValue);
        Assert.AreEqual(2, sample["a"].NullableIntValue);
        Assert.AreEqual("abc", sample["a"].Text);
        Assert.AreEqual(1, sample["a"].Nested.IntValue);
        Assert.AreEqual(22, sample["a"].Nested.NullableIntValue);
        Assert.AreEqual("Wow!", sample["a"].Nested.Text);
        Assert.AreSame(overrides["a"].Nested.Nested, sample["a"].Nested.Nested);

        Assert.AreEqual(101, sample["b"].IntValue);
        Assert.IsNull(sample["b"].NullableIntValue);
        Assert.AreEqual("xyz", sample["b"].Text);
        Assert.AreEqual(102, sample["b"].Nested.IntValue);
        Assert.AreEqual(2, sample["b"].Nested.NullableIntValue);
        Assert.IsNull(sample["b"].Nested.Text);
        Assert.IsNull(sample["b"].Nested.Nested);

        Assert.AreSame(overrides["c"], sample["c"]);
    }
}
