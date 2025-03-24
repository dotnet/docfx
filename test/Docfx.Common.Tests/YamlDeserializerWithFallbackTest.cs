// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;

namespace Docfx.Common.Tests;

[TestClass]
public class YamlDeserializerWithFallbackTest
{
    [TestMethod]
    public void TestYamlDeserializerWithFallback()
    {
        var deserializer = YamlDeserializerWithFallback.Create<string>()
            .WithFallback<List<string>>();
        {
            var obj = deserializer.Deserialize(() => new StringReader("A"));
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType<string>(obj);
            Assert.AreEqual("A", (string)obj);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader(@"- A
- B"));
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType<List<string>>(obj);
            var a = (List<string>)obj;
            Assert.AreEqual("A", a[0]);
            Assert.AreEqual("B", a[1]);
        }
        {
            var ex = Assert.Throws<YamlException>(() => deserializer.Deserialize(() => new StringReader(@"- A
- A: abc")));
            Assert.AreEqual(2, ex.Start.Line);
            Assert.AreEqual(3, ex.Start.Column);
        }
    }

    [TestMethod]
    public void TestYamlDeserializerWithFallback_MultiFallback()
    {
        var deserializer = YamlDeserializerWithFallback.Create<int>()
            .WithFallback<string>()
            .WithFallback<string[]>();
        {
            var obj = deserializer.Deserialize(() => new StringReader("1"));
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType<int>(obj);
            Assert.AreEqual(1, (int)obj);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader("A"));
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType<string>(obj);
            Assert.AreEqual("A", (string)obj);
        }
        {
            var obj = deserializer.Deserialize(() => new StringReader(@"- A
- B"));
            Assert.IsNotNull(obj);
            Assert.IsInstanceOfType<string[]>(obj);
            var a = (string[])obj;
            Assert.AreEqual("A", a[0]);
            Assert.AreEqual("B", a[1]);
        }
        {
            var ex = Assert.Throws<YamlException>(() => deserializer.Deserialize(() => new StringReader(@"- A
- A: abc")));
            Assert.AreEqual(2, ex.Start.Line);
            Assert.AreEqual(3, ex.Start.Column);
        }
    }
}
