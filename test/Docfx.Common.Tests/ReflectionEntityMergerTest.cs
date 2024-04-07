// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;
using Xunit;

namespace Docfx.Common.Tests;

[Trait("Related", "ReflectionEntityMerger")]
public class ReflectionEntityMergerTest
{
    [Fact]
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
        Assert.Equal(10, sample.IntValue);
        Assert.Equal(2, sample.NullableIntValue);
        Assert.Equal("abc", sample.Text);
        Assert.Equal(1, sample.Nested.IntValue);
        Assert.Equal(22, sample.Nested.NullableIntValue);
        Assert.Equal("Wow!", sample.Nested.Text);
        Assert.Same(overrides.Nested.Nested, sample.Nested.Nested);
    }

    public class BasicSample
    {
        public int IntValue { get; set; }
        public int? NullableIntValue { get; set; }
        public string Text { get; set; }
        public BasicSample Nested { get; set; }
    }

    [Fact]
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
        Assert.Equal(10, sample.IntValue);
        Assert.Equal(2, sample.NullableIntValue);
        Assert.Null(sample.Text);
        Assert.Same(overrides.Array1, sample.Array1);
        Assert.Null(sample.Array2);
        Assert.Equal(0, sample.Nested.IntValue);
        Assert.Equal(2, sample.Nested.NullableIntValue);
        Assert.Equal("Wow!", sample.Nested.Text);
        Assert.Null(sample.Nested.Nested);
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

    [Fact]
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
        Assert.Equal("O1", sample[0].Text);
        Assert.Equal("O2->N2", sample[1].Text);
        Assert.Equal("O3->N3", sample[2].Text);
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

    [Fact]
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

        Assert.Equal(10, sample["a"].IntValue);
        Assert.Equal(2, sample["a"].NullableIntValue);
        Assert.Equal("abc", sample["a"].Text);
        Assert.Equal(1, sample["a"].Nested.IntValue);
        Assert.Equal(22, sample["a"].Nested.NullableIntValue);
        Assert.Equal("Wow!", sample["a"].Nested.Text);
        Assert.Same(overrides["a"].Nested.Nested, sample["a"].Nested.Nested);

        Assert.Equal(101, sample["b"].IntValue);
        Assert.Null(sample["b"].NullableIntValue);
        Assert.Equal("xyz", sample["b"].Text);
        Assert.Equal(102, sample["b"].Nested.IntValue);
        Assert.Equal(2, sample["b"].Nested.NullableIntValue);
        Assert.Null(sample["b"].Nested.Text);
        Assert.Null(sample["b"].Nested.Nested);

        Assert.Same(overrides["c"], sample["c"]);
    }
}
