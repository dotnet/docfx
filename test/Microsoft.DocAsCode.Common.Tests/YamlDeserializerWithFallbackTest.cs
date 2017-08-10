// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using YamlDotNet.Core;

    [Trait("Owner", "zhyan")]
    public class YamlDeserializerWithFallbackTest
    {
        [Fact]
        public void TestYamlDeserializerWithFallback()
        {
            var deserialzer = YamlDeserializerWithFallback.Create<string>()
                .WithFallback<List<string>>();
            {
                var obj = deserialzer.Deserialize(() => new StringReader(@"A"));
                Assert.NotNull(obj);
                var a = Assert.IsType<string>(obj);
                Assert.Equal("A", a);
            }
            {
                var obj = deserialzer.Deserialize(() => new StringReader(@"- A
- B"));
                Assert.NotNull(obj);
                var a = Assert.IsType<List<string>>(obj);
                Assert.Equal("A", a[0]);
                Assert.Equal("B", a[1]);
            }
            {
                var ex = Assert.Throws<YamlException>(() => deserialzer.Deserialize(() => new StringReader(@"- A
- A: abc")));
                Assert.Equal(2, ex.Start.Line);
                Assert.Equal(3, ex.Start.Column);
            }
        }

        [Fact]
        public void TestYamlDeserializerWithFallback_MultiFallback()
        {
            var deserialzer = YamlDeserializerWithFallback.Create<int>()
                .WithFallback<string>()
                .WithFallback<string[]>();
            {
                var obj = deserialzer.Deserialize(() => new StringReader(@"1"));
                Assert.NotNull(obj);
                var a = Assert.IsType<int>(obj);
                Assert.Equal(1, a);
            }
            {
                var obj = deserialzer.Deserialize(() => new StringReader(@"A"));
                Assert.NotNull(obj);
                var a = Assert.IsType<string>(obj);
                Assert.Equal("A", a);
            }
            {
                var obj = deserialzer.Deserialize(() => new StringReader(@"- A
- B"));
                Assert.NotNull(obj);
                var a = Assert.IsType<string[]>(obj);
                Assert.Equal("A", a[0]);
                Assert.Equal("B", a[1]);
            }
            {
                var ex = Assert.Throws<YamlException>(() => deserialzer.Deserialize(() => new StringReader(@"- A
- A: abc")));
                Assert.Equal(2, ex.Start.Line);
                Assert.Equal(3, ex.Start.Column);
            }
        }
    }
}
