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
                (s, o) => new string(s.ToCharArray().Union(o.ToCharArray()).OrderBy(c => c).ToArray()));

            Assert.Same(string.Empty, m.RootState.Value);
            Assert.Same(m.RootState, m.RootState.Transit(string.Empty));

            // "" + "A" => "A"
            var a = m.RootState.Transit("A");
            Assert.Equal("A", a.Value);
            Assert.Same(a, m.RootState.Transit("A"));

            // "" + "B" => "B"
            var b = m.RootState.Transit("B");
            Assert.Equal("B", b.Value);
            Assert.Same(b, m.RootState.Transit("B"));

            // "" + "AB" => "AB"
            var ab = m.RootState.Transit("AB");
            Assert.Equal("AB", ab.Value);
            Assert.Same(ab, m.RootState.Transit("AB"));

            // "A" + "B" => "AB"
            Assert.Same(ab, a.Transit("B"));
            // "B" + "A" => "AB"
            Assert.Same(ab, b.Transit("A"));
            // "AB" + "A" => "AB"
            Assert.Same(ab, a.Transit("AB"));
            // "AB" + "B" => "AB"
            Assert.Same(ab, b.Transit("AB"));
            // "AB" + "AB" => "AB"
            Assert.Same(ab, ab.Transit("AB"));
            // "AB" + "A" => "AB"
            Assert.Same(ab, ab.Transit("A"));
            // "AB" + "B" => "AB"
            Assert.Same(ab, ab.Transit("B"));

            Assert.Same(ab, m.RootState.Transit("AB"));
            Assert.Same(ab, m.RootState.Transit("AB").Transit(string.Empty));
            Assert.Same(ab, m.RootState.Transit("BA"));
            Assert.Same(ab, m.RootState.Transit("BA").Transit(string.Empty));
            Assert.Same(ab, m.RootState.Transit("A").Transit(string.Empty).Transit("B"));
            Assert.Same(ab, m.RootState.Transit("B").Transit(string.Empty).Transit("A"));
        }
    }
}
