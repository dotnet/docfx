// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "StateMachine")]
    public class StateMachineTest
    {
        [Fact]
        public void TestStateMachine()
        {
            var m = new StateMachine<string, string>(
                string.Empty,
                (s, o) => new string(s.ToCharArray().Concat(o.ToCharArray()).OrderBy(c => c).ToArray()));

            Assert.Same(string.Empty, m.RootState.Value);
            Assert.Same(m.RootState, m.RootState.Transit(string.Empty));

            var a = m.RootState.Transit("A");
            Assert.Equal("A", a.Value);
            Assert.Same(a, m.RootState.Transit("A"));

            var b = m.RootState.Transit("B");
            Assert.Equal("B", b.Value);

            var ab = m.RootState.Transit("AB");
            Assert.Equal("AB", ab.Value);

            Assert.Same(ab, a.Transit("B"));
            Assert.Same(ab, b.Transit("A"));

            Assert.Same(ab, m.RootState.Transit("AB"));
            Assert.Same(ab, m.RootState.Transit("AB").Transit(string.Empty));
            Assert.Same(ab, m.RootState.Transit("BA"));
            Assert.Same(ab, m.RootState.Transit("BA").Transit(string.Empty));
            Assert.Same(ab, m.RootState.Transit("A").Transit(string.Empty).Transit("B"));
            Assert.Same(ab, m.RootState.Transit("B").Transit(string.Empty).Transit("A"));
        }
    }
}
