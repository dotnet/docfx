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
        public void KitchenSink()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: zone target=""chromeless""
## Section for chromeless only

Some chromeless-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: zone-end

## Section 2

Shared content.

:::zone
default target
:::zone-end

:::  zone  pivot=""foo""
a pivot
:::    zone-end

:::  zone  pivot="" foo bar ""    target=""docs""
a pivot with target 
:::    zone-end

::: zone target=""docs"" pivot=""csharp7-is-great""
hello
::: zone-end
";

            var marked = TestUtility.Markup(content, "fake.md");

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div data-zone=""chromeless"" sourceFile=""fake.md"" sourceStartLineNumber=""9"">
<h2 id=""section-for-chromeless-only"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for chromeless only</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some chromeless-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
<div data-zone=""docs"" sourceFile=""fake.md"" sourceStartLineNumber=""23"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""24"">default target</p>
</div>
<div data-zone=""docs"" data-pivot=""foo"" sourceFile=""fake.md"" sourceStartLineNumber=""27"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""28"">a pivot</p>
</div>
<div data-zone=""docs"" data-pivot="" foo bar "" sourceFile=""fake.md"" sourceStartLineNumber=""31"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""32"">a pivot with target</p>
</div>
<div data-zone=""docs"" data-pivot=""csharp7-is-great"" sourceFile=""fake.md"" sourceStartLineNumber=""35"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""36"">hello</p>
</div>
".Replace("\r\n", "\n");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        public void AttributeMissingClosingQuote()
        {
            //arange
            var source = @"::: zone target=""chromeless";

            // assert
            var expected = @"<p>::: zone target=&quot;chromeless</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid zone attribute \"target\". Values must be terminated with a double quote.", listener.Items[0].Message);
        }

        [Fact]
        public void MissingEndTag()
        {
            //arange
            var source1 = @"::: zone target=""chromeless""";
            var source2 = @"::: zone target=""chromeless""
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
            Assert.Equal("No \"::: zone-end\" found. Blocks should be explicitly closed.", listener.Items[0].Message);
        }

        [Fact]
        public void NotNested()
        {
            //arange
            var content = @"::: zone target=""chromeless""
::: zone target=""pdf""
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
            Assert.Equal("Zones cannot be nested.", listener.Items[0].Message);
        }

        [Fact]
        public void NoOverlap()
        {
            //arange
            var content = @"::: zone target=""chromeless""
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

            Assert.Equal("Invalid zone. A zone cannot end before blocks nested within it have ended.", listener.Items.First(x => x.Code == "invalid-zone").Message);
        }
        
        [Fact]
        public void PdfPivotInvalid()
        {
            //arange
            var source = @"::: zone target = ""pdf""  pivot = ""foo""  ";

            // assert
            var expected = @"<p>::: zone target = &quot;pdf&quot;  pivot = &quot;foo&quot;</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid zone. Pivot not permitted in pdf target.", listener.Items[0].Message);
        }

        [Fact]
        public void UnexpectedAttribute()
        {
            //arange
            var source = @"::: zone target=""pdf"" something";

            // assert
            var expected = @"<p>::: zone target=&quot;pdf&quot; something</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid zone. Unexpected attribute \"something\".", listener.Items[0].Message);
        }

        [Fact]
        public void DuplicateAttribute()
        {
            //arange
            var source = @"::: zone target=""pdf"" target=""docs""";

            // assert
            var expected = @"<p>::: zone target=&quot;pdf&quot; target=&quot;docs&quot;</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid zone. Attribute \"target\" specified multiple times.", listener.Items[0].Message);
        }

        [Fact]
        public void InvalidAttribute()
        {
            //arange
            var source = @"::: zone *=""pdf""";

            // assert
            var expected = @"<p>::: zone *=&quot;pdf&quot;</p>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid zone attribute.", listener.Items[0].Message);
        }

        [Fact]
        public void TextAfterEndTag()
        {
            //arange
            var source = @":::zone
:::zone-end asdjklf";

            // assert
            var expected = @"<div data-zone=""docs"">
</div>
";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            Assert.Single(listener.Items);
            Assert.Equal("Invalid character after \"::: zone-end\": \"a\".", listener.Items[0].Message);
        }
    }
}
