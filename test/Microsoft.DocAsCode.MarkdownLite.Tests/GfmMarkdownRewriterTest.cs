// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class GfmMarkdownRewriterTest
    {
        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_LinkWithSpecialCharactorsInTitle()
        {
            var source = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.md ""title is ""hello"" world."")";
            var expected = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.md ""title is ""hello"" world."")

";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_RefLink()
        {
            var source = @"This is Ref Link: [Simple text][A]
[A]: https://www.google.com";
            var expected = @"This is Ref Link: [Simple text][A]

[A]: https://www.google.com";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_RefLinkWithSimpleStyle()
        {
            var source = @"This is Ref Link: [A][]
[A]: https://www.google.com";
            var expected = @"This is Ref Link: [A][A]

[A]: https://www.google.com";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_NumberLink()
        {
            var source = @"This is Ref Link: [NumberLink]
[NumberLink]: https://www.google.com";
            var expected = @"This is Ref Link: [NumberLink]

[NumberLink]: https://www.google.com";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_AutoLink()
        {
            var source = @"This is Auto Link: <https://www.google.com>";
            var expected = @"This is Auto Link: <https://www.google.com>

";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_AutoLinkWithMail()
        {
            var source = @"This is Auto Link: <user@microsoft.com>";
            var expected = @"This is Auto Link: <user@microsoft.com>

";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_UrlLink()
        {
            var source = @"This is Url Link: https://www.google.com";
            var expected = @"This is Url Link: https://www.google.com

";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_ImageLinkWithSpecialCharactorsInTitle()
        {
            var source = @"![This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is ""hello"" world."")";
            var expected = @"![This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is ""hello"" world."")

";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_RefImageLink()
        {
            var source = @"This is Ref Image Link: ![Simple image text][A]
[A]: girl.png";
            var expected = @"This is Ref Image Link: ![Simple image text][A]

[A]: girl.png";
            TestGfmRewriterInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "MarkdownRewriter")]
        public void TestGfmRewriter_NumberImageLink()
        {
            var source = @"This is Ref Image Link: ![NumberImageLink]
[NumberImageLink]: girl.png";
            var expected = @"This is Ref Image Link: ![NumberImageLink]

[NumberImageLink]: girl.png";
            TestGfmRewriterInGeneral(source, expected);
        }

        public void TestGfmRewriterInGeneral(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new MarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
