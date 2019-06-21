// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class HtmlUtilityTest
    {
        [Theory]

        // Add link type
        [InlineData("<a href='a.md' />", "<a href='a.md' data-linktype='relative-path' />")]
        [InlineData("<a href='(https://a)' />", "<a href='(https://a)' data-linktype='relative-path' />")]
        [InlineData("<a href='#aA' />", "<a href='#aA' data-linktype='self-bookmark' />")]
        [InlineData("<a href='/a' />", "<a href='/zh-cn/a' data-linktype='absolute-path' />")]
        [InlineData("<a href='/Alink#fraGMENT' />", "<a href='/zh-cn/Alink#fraGMENT' data-linktype='absolute-path' />")]
        [InlineData("<a href='/Alink?quERY' />", "<a href='/zh-cn/Alink?quERY' data-linktype='absolute-path' />")]
        [InlineData("<a href='/a#x' />", "<a href='/zh-cn/a#x' data-linktype='absolute-path' />")]
        [InlineData("<a href='\\a#x' />", "<a href='/zh-cn\\a#x' data-linktype='absolute-path' />")]
        [InlineData("<a href='/de-de/a' />", "<a href='/de-de/a' data-linktype='absolute-path' />")]
        [InlineData("<a href='/xxx-yyy/a' />", "<a href='/zh-cn/xxx-yyy/a' data-linktype='absolute-path' />")]
        [InlineData("<a href='http://abc' />", "<a href='http://abc' data-linktype='external' />")]
        [InlineData("<a href='https://abc' />", "<a href='https://abc' data-linktype='external' />")]
        [InlineData("<a href='https://[abc]' />", "<a href='https://[abc]' data-linktype='external' />")]

        // Rerun codepen iframes
        [InlineData("<div></div>", "<div></div>")]
        [InlineData("<iframe></iframe>", "<iframe></iframe>")]
        [InlineData("<iframe src='//codepen.io/a' />", "<iframe src='//codepen.io/a&rerun-position=hidden&' />")]

        // Strip tags
        [InlineData("<style href='a'>", "<div></div>")]
        [InlineData("<div style='a'></div>", "<div></div>")]
        [InlineData("<div><style href='a'></div>", "<div></div>")]
        [InlineData("<div><link href='a'></div>", "<div></div>")]
        [InlineData("<div><script></script></div>", "<div></div>")]
        public void HtmlPostProcess(string input, string output)
        {
            var actual = HtmlUtility.LoadHtml(input).HtmlPostProcess(new CultureInfo("zh-CN"));

            Assert.Equal(TestUtility.NormalizeHtml(output), TestUtility.NormalizeHtml(actual));
        }

        [Theory]
        [InlineData("", "666", "")]
        [InlineData("</a>", "666", "</a>")]
        [InlineData("<a href='hello'>", "666", "<a href='666'>")]
        [InlineData("<a href='hello'>", null, "<a href=''>")]
        [InlineData("<a href='hello'>", "~!@#$%^&*()<>?:,./][{}|", "<a href='~!@#$%^&amp;*()&lt;&gt;?:,./][{}|'>")]
        [InlineData("<A hrEf=''>", "666", "<A hrEf='666'>")]
        [InlineData("<a href = 'hello'>", "666", "<a href = '666'>")]
        [InlineData("<a   target='_blank'   href='h'>", "666", "<a   target='_blank'   href='666'>")]
        [InlineData("<img src='a/b.png' />", "666", "<img src='666' />")]
        [InlineData("<iMg sRc = 'a/b.png' />", "666", "<iMg sRc = '666' />")]
        [InlineData("<div><a href='hello'><img src='a/b.png' /></div>", "666", "<div><a href='666'><img src='666' /></div>")]
        [InlineData("<div><img src='a/b.png' /><a href='hello'></div>", "666", "<div><img src='666' /><a href='666'></div>")]
        public void TransformLinks(string input, string link, string output)
        {
            Assert.Equal(output, HtmlUtility.TransformLinks(input, (href, _) => link));
        }

        [Theory]
        [InlineData("", "a", "a", "")]
        [InlineData("<a href='hello'>", "a", "a", "<a href='hello'>")]
        [InlineData("<xref href='hello'>", "a", "b", "<a href='a'>b</a>")]
        [InlineData(@"<xref href='hello' data-raw-html='@higher&amp;' data-raw-source='@lower'>", "", "", @"@higher&")]
        [InlineData(@"<xref href='hello' data-raw-source='@lower&amp;'>", "", "", @"@lower&")]
        [InlineData(@"<xref href='a&amp;b' data-raw-source='@lower&amp;'>", "c&d", "", @"<a href='c&amp;d'></a>")]
        public void TransformXrefs(string input, string xref, string display, string output)
        {
            var (_, result) = HtmlUtility.TransformXref(input, default, string.Empty, _ => (default, xref, display, default));
            Assert.Equal(output, result);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("a", 1)]
        [InlineData("a b", 2)]
        [InlineData("a b ?!", 2)]
        [InlineData(@"<p>a</p>b", 2)]
        [InlineData(@"<p>a</p>b<p>c</p>", 3)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information ?!</h1></div></div>", 4)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1></div></div>", 4)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
        [InlineData(@"<div><title>Connect and TFS information</title><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 39)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the <a href=""http://www.msdn.com"">MSDN</a> and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
        public static void CountWord(string html, long expectedCount)
        {
            Assert.Equal(expectedCount, HtmlUtility.CountWord(HtmlUtility.LoadHtml(html)));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("<h1 id='a'></h1>", "a")]
        [InlineData("<h1 id='a'></h1><h2 id='b'></h2>", "a, b")]
        [InlineData("<a id='a'></a>", "a")]
        [InlineData("<a name='a'></a>", "a")]
        public static void GetBookmarks(string html, string expected)
        {
            Assert.Equal(expected, string.Join(", ", HtmlUtility.GetBookmarks(HtmlUtility.LoadHtml(html))));
        }
    }
}
