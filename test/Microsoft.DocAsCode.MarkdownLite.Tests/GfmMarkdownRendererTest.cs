// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    [Trait("Related", "MarkdownRenderer")]
    public class GfmMarkdownRendererTest
    {
        [Fact]
        public void TestGfmRenderer_LinkWithSpecialCharactorsInTitle()
        {
            var source = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.md ""title is \""hello\"" world."")";
            var expected = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.md ""title is \""hello\"" world."")

";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_RefLink()
        {
            var source = @"This is Ref Link: [Simple text][A]
[A]: https://www.google.com";
            var expected = @"This is Ref Link: [Simple text][A]

[A]: https://www.google.com";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_RefLinkWithSimpleStyle()
        {
            var source = @"This is Ref Link: [A][]
[A]: https://www.google.com";
            var expected = @"This is Ref Link: [A][A]

[A]: https://www.google.com";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_NumberLink()
        {
            var source = @"This is Ref Link: [NumberLink]
[NumberLink]: https://www.google.com";
            var expected = @"This is Ref Link: [NumberLink]

[NumberLink]: https://www.google.com";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        [Trait("Disable", "Because in GFM the mail will be encrypt. Disable this case as it will fail.")]
        public void TestGfmRenderer_AutoLink()
        {
            var source = @"This is Auto Link: <https://www.google.com>";
            var expected = @"This is Auto Link: <https://www.google.com>

";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        public void TestGfmRenderer_AutoLinkWithMail()
        {
            var source = @"This is Auto Link: <user@microsoft.com>";
            var expected = @"This is Auto Link: <user@microsoft.com>

";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_UrlLink()
        {
            var source = @"This is Url Link: https://www.google.com";
            var expected = @"This is Url Link: https://www.google.com

";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_ImageLinkWithSpecialCharactorsInTitle()
        {
            var source = @"![This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is \""hello\"" world."")";
            var expected = @"![This is link text with quotation \' and double quotation \""hello\"" world](girl.png ""title is \""hello\"" world."")

";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_RefImageLink()
        {
            var source = @"This is Ref Image Link: ![Simple image text][A]
[A]: girl.png";
            var expected = @"This is Ref Image Link: ![Simple image text][A]

[A]: girl.png";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        [Fact]
        public void TestGfmRenderer_NumberImageLink()
        {
            var source = @"This is Ref Image Link: ![NumberImageLink]
[NumberImageLink]: girl.png";
            var expected = @"This is Ref Image Link: ![NumberImageLink]

[NumberImageLink]: girl.png";
            TestGfmRendererInGeneral(source, expected);
            TestGfmRendererInGeneral(expected, expected);
        }

        private void TestGfmRendererInGeneral(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new MarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
