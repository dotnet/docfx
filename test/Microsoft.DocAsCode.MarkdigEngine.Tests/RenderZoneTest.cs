// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;
    using System.Linq;

    public class RenderZoneTest
    {
        static public string LoggerPhase = "RenderZone";

        [Fact]
        public void RenderZoneTestGeneral()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: zone render=""chromeless""
## Section for chromeless only

Some chromeless-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: zone-end

## Section 2

Shared content.
";

            var marked = TestUtility.Markup(content, "fake.md");

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div sourceFile=""fake.md"" sourceStartLineNumber=""9"" data-zone=""chromeless"">
<h2 id=""section-for-chromeless-only"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for chromeless only</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some chromeless-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
".Replace("\r\n", "\n");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        public void RenderZoneTestInvalid()
        {
            //arange
            var source = @"::: zone render=""chromeless";

            // assert
            var expected = @"<p>::: zone render=&quot;chromeless</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Zone render does not have ending character (\").", listener.Items[0].Message);
        }

        [Fact]
        public void RenderZoneTestNotClosed()
        {
            //arange
            var source1 = @"::: zone render=""chromeless""";
            var source2 = @"::: zone render=""chromeless""
::: zone-end";

            // assert
            var expected = @"<div data-zone=""chromeless"">
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
            Assert.Equal("No \"::: zone-end\" found for \"chromeless\", zone does not end explictly.", listener.Items[0].Message);
        }

        [Fact]
        public void RenderZoneTestNotNested()
        {
            //arange
            var content = @"::: zone render=""chromeless""
::: zone render=""pdf""
::: zone-end
::: zone-end
";

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Zone render cannot be nested.", listener.Items[0].Message);
        }

        [Fact]
        public void RenderZoneTestNoOverlap()
        {
            //arange
            var content = @"::: zone render=""chromeless""
::: moniker range=""start""
::: zone-end
::: moniker-end
";

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            Assert.Equal("Invalid stack order. A render zone cannot end before other nested blocks have ended.", listener.Items.First(x => x.Code == "invalid-render-zone").Message);
        }
    }
}
