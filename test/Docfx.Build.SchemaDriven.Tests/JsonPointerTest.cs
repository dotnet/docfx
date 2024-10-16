// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Exceptions;
using Docfx.Tests.Common;
using FluentAssertions;
using Xunit;

namespace Docfx.Build.SchemaDriven.Tests;
public class JsonPointerTest : TestBase
{
    [Fact]
    public void TestJsonPointerSpec()
    {
        var root = ConvertToObjectHelper.ConvertToDynamic(ConvertToObjectHelper.ConvertJObjectToObject(JsonUtility.FromJsonString<object>(
            """
            {
              "foo": ["bar", "baz"],
              "": 0,
              "a/b": 1,
              "c%d": 2,
              "e^f": 3,
              "g|h": 4,
              "i\\j": 5,
              "k\"l": 6,
              " ": 7,
              "m~n": 8
            }
            """)));

        new JsonPointer("").GetValue(root).Should().Be(root);
        new JsonPointer("/foo").GetValue(root).Should().Be(((dynamic)root).foo);
        new JsonPointer("/foo/0").GetValue(root).Should().Be("bar");
        new JsonPointer("/").GetValue(root).Should().Be(0);
        new JsonPointer("/a~1b").GetValue(root).Should().Be(1);
        new JsonPointer("/c%d").GetValue(root).Should().Be(2);
        new JsonPointer("/e^f").GetValue(root).Should().Be(3);
        new JsonPointer("/g|h").GetValue(root).Should().Be(4);
        new JsonPointer("/i\\j").GetValue(root).Should().Be(5);
        new JsonPointer("/k\"l").GetValue(root).Should().Be(6);
        new JsonPointer("/ ").GetValue(root).Should().Be(7);
        new JsonPointer("/m~0n").GetValue(root).Should().Be(8);
    }

    [Fact]
    public void TestJsonPointerWithComplexObject()
    {
        var root = ConvertToObjectHelper.ConvertToDynamic(ConvertToObjectHelper.ConvertJObjectToObject(JsonUtility.FromJsonString<object>(@"
{
      ""dict"": {
        ""key1"": ""value1"",
        ""key2"": [""arr1"", ""arr2""],
        ""key3"": {
            ""key1"": ""value1"",
            ""key2"": [""arr1"", ""arr2""],
            ""key3"": {
                ""key1"": ""value1"",
                ""key2"": [""arr1"", ""arr2""],
                ""key3"": {
                   ""key1"": ""value1""
                }
            }
        }
    },
      ""array"": [""bar"", ""baz""]
   }
")));

        Assert.Equal(root, new JsonPointer("").GetValue(root));
        Assert.Equal("value1", new JsonPointer("/dict/key1").GetValue(root));
        Assert.Equal("arr2", new JsonPointer("/dict/key2/1").GetValue(root));
        Assert.Equal("value1", new JsonPointer("/dict/key3/key3/key3/key1").GetValue(root));
        Assert.Null(new JsonPointer("/dict/key4").GetValue(root));
        Assert.Null(new JsonPointer("/dict/key4/key1").GetValue(root));
        Assert.Null(new JsonPointer("/dict/key2/2").GetValue(root));

        var jp = new JsonPointer("/dict/key1");
        jp.SetValue(ref root, 1);
        Assert.Equal(1, jp.GetValue(root));

        jp = new JsonPointer("/dict/key3/key2/1");
        jp.SetValue(ref root, 2);
        Assert.Equal(2, jp.GetValue(root));

        jp = new JsonPointer("");
        jp.SetValue(ref root, 3);
        Assert.Equal(3, root);
        Assert.Equal(3, jp.GetValue(root));

        Assert.Throws<InvalidJsonPointerException>(() => new JsonPointer("/dict/key2/2").SetValue(ref root, 1));
    }
}
