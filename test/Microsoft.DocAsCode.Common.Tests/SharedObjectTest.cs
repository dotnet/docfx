// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "SharedObject")]
    public class SharedObjectTest
    {
        [Fact]
        public void TestSharedObject()
        {
            var m = new SharedObjectManager<string, string>(
                string.Empty,
                (s, o) => new string(s.ToCharArray().Concat(o.ToCharArray()).OrderBy(c => c).ToArray()));

            Assert.Same(string.Empty, m.RootState.Value);
            Assert.Same(m.RootState, m.RootState.Modify(string.Empty));

            var a = m.RootState.Modify("A");
            Assert.Equal("A", a.Value);
            Assert.Same(a, m.RootState.Modify("A"));

            var b = m.RootState.Modify("B");
            Assert.Equal("B", b.Value);

            var ab = m.RootState.Modify("AB");
            Assert.Equal("AB", ab.Value);

            Assert.Same(ab, a.Modify("B"));
            Assert.Same(ab, b.Modify("A"));

            Assert.Same(ab, m.RootState.Modify("AB"));
            Assert.Same(ab, m.RootState.Modify("AB").Modify(string.Empty));
            Assert.Same(ab, m.RootState.Modify("BA"));
            Assert.Same(ab, m.RootState.Modify("BA").Modify(string.Empty));
            Assert.Same(ab, m.RootState.Modify("A").Modify(string.Empty).Modify("B"));
            Assert.Same(ab, m.RootState.Modify("B").Modify(string.Empty).Modify("A"));
        }
    }
}
