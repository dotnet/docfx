// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class MonikerRangeTest
    {
        static public string LoggerPhase = "MonikerRange";

        [Fact]
        public void MonikerRangeTestGeneral()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: moniker range="">= myproduct-4.1""
## Section for myproduct-4.1 and Later

Some version-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: moniker-end

## Section 2

Shared content.
";

            var marked = TestUtility.Markup(content, "fake.md");

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div sourceFile=""fake.md"" sourceStartLineNumber=""9"" range="">= myproduct-4.1"">
<h2 id=""section-for-myproduct-41-and-later"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for myproduct-4.1 and Later</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some version-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
".Replace("\r\n", "\n");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }


        [Fact]
        public void MonikerRangeTestInvalid()
        {
            //arange
            var source = @"::: moniker range=""azure-rest-1.0";

            // assert
            var expected = @"<p>::: moniker range=&quot;azure-rest-1.0</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("MonikerRange does not have ending charactor (\").", listener.Items[0].Message);
        }

        [Fact]
        public void MonikerRangeTestNotClosed()
        {
            //arange
            var source1 = @"::: moniker range=""start""";
            var source2 = @"::: moniker range=""start""
::: moniker-end";

            // assert
            var expected = @"<div range=""start"">
</div>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source2, TestUtility.MarkupWithoutSourceInfo);

                Assert.Empty(listener.Items);

                TestUtility.AssertEqual(expected, source1, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("No \"::: moniker-end\" found for \"start\", MonikerRange does not end explictly.", listener.Items[0].Message);
        }

        [Fact]
        public void MonikerRangeWithCodeIndent()
        {
            var source = @"::: moniker range=""start""
    console.log(""hehe"")
::: moniker-end";
            var expected = @"<div range=""start"">
<pre><code>console.log(&quot;hehe&quot;)
</code></pre>
</div>
";
            TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
        }
    }
}
