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
            var a = m.RootState.Modify("A");
            Assert.Same(a, m.RootState.Modify("A"));
            var ab = a.Modify("B");
            var b = m.RootState.Modify("B");
            Assert.Same(ab, b.Modify("A"));
            Assert.Same(ab, b.Modify("A"));
            Assert.Same(ab, m.RootState.Modify("AB"));
            Assert.Same(ab, m.RootState.Modify("AB"));
            Assert.Same(ab, m.RootState.Modify("BA"));
            Assert.Same(ab, m.RootState.Modify("BA"));
        }
    }
}
