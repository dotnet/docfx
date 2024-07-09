// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using YamlDotNet.Core;

namespace Docfx.Common.Tests;

public class YamlDeserializerWithFallbackTest
{
    [Fact]
    public void TestYamlDeserializerWithFallback()
    {
        var deserializer = YamlDeserializerWithFallback.Create<string>()
            .WithFallback<List<string>>();
        {
            var obj = deserializer.Deserialize(() => new StringReader("A"));
            Assert.NotNull(obj);
            var a = Assert.IsType<string>(obj);
            Assert.Equal("A", a);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader(@"- A
- B"));
            Assert.NotNull(obj);
            var a = Assert.IsType<List<string>>(obj);
            Assert.Equal("A", a[0]);
            Assert.Equal("B", a[1]);
        }
        {
            var ex = Assert.Throws<YamlException>(() => deserializer.Deserialize(() => new StringReader(@"- A
- A: abc")));
            Assert.Equal(2, ex.Start.Line);
            Assert.Equal(3, ex.Start.Column);
        }
    }

    [Fact]
    public void TestYamlDeserializerWithFallback_MultiFallback()
    {
        var deserializer = YamlDeserializerWithFallback.Create<int>()
            .WithFallback<string>()
            .WithFallback<string[]>();
        {
            var obj = deserializer.Deserialize(() => new StringReader("1"));
            Assert.NotNull(obj);
            var a = Assert.IsType<int>(obj);
            Assert.Equal(1, a);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader("A"));
            Assert.NotNull(obj);
            var a = Assert.IsType<string>(obj);
            Assert.Equal("A", a);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader(@"- A
- B"));
            Assert.NotNull(obj);
            var a = Assert.IsType<string[]>(obj);
            Assert.Equal("A", a[0]);
            Assert.Equal("B", a[1]);
        }
        {
            var ex = Assert.Throws<YamlException>(() => deserializer.Deserialize(() => new StringReader(@"- A
- A: abc")));
            Assert.Equal(2, ex.Start.Line);
            Assert.Equal(3, ex.Start.Column);
        }
    }
}
