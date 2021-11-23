// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using HtmlReaderWriter;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

public class HtmlUtilityTest
{
    [Theory]
    [InlineData("<a href='a.md' />", "<a href='a.md' data-linktype='relative-path' />")]
    [InlineData("<a href='(https://a)' />", "<a href='(https://a)' data-linktype='relative-path' />")]
    [InlineData("<a href='#aA' />", "<a href='#aA' data-linktype='self-bookmark' />")]
    [InlineData("<a href='/a' />", "<a href='/zh-cn/a' data-linktype='absolute-path' />")]
    [InlineData("<a href='/Alink#fraGMENT' />", "<a href='/zh-cn/Alink#fraGMENT' data-linktype='absolute-path' />")]
    [InlineData("<a href='/Alink?quERY' />", "<a href='/zh-cn/Alink?quERY' data-linktype='absolute-path' />")]
    [InlineData("<a href='/a#x' />", "<a href='/zh-cn/a#x' data-linktype='absolute-path' />")]
    [InlineData("<a href='\\a#x' />", "<a href='/zh-cn\\a#x' data-linktype='absolute-path' />")]
    [InlineData("<a href='/de-de/a' />", "<a href='/de-de/a' data-linktype='absolute-path' />")]
    [InlineData("<a href='/x-y/a' />", "<a href='/zh-cn/x-y/a' data-linktype='absolute-path' />")]
    [InlineData("<a href='http://abc' />", "<a href='http://abc' data-linktype='external' />")]
    [InlineData("<a href='https://abc' />", "<a href='https://abc' data-linktype='external' />")]
    [InlineData("<a href='https://[abc]' />", "<a href='https://[abc]' data-linktype='relative-path' />")]
    public void HtmlAddLinkType(string input, string output)
    {
        var actual = HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                HtmlUtility.AddLinkType(ErrorBuilder.Null, new("a.md"), ref token, "zh-cn", new()));

        Assert.Equal(JsonDiff.NormalizeHtml(output), JsonDiff.NormalizeHtml(actual));
    }

    [Theory]
    [InlineData("<div></div>", "<div></div>")]
    [InlineData("<iframe></iframe>", "<iframe></iframe>")]
    [InlineData("<iframe src='//codepen.io/a' />", "<iframe src='//codepen.io/a&rerun-position=hidden&' />")]
    public void HtmlRemoveRerunCodepenIframes(string input, string output)
    {
        var actual = HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) => HtmlUtility.RemoveRerunCodepenIframes(ref token));

        Assert.Equal(JsonDiff.NormalizeHtml(output), JsonDiff.NormalizeHtml(actual));
    }

    [Theory]
    [InlineData("<style href='a'>", "")]
    [InlineData("<div style='a'></div>", "<div></div>")]
    [InlineData("<div><style href='a'></div>", "<div></div>")]
    [InlineData("<div><link href='a'></div>", "<div></div>")]
    [InlineData("<div><script></script></div>", "<div></div>")]
    public void HtmlStripTags(string input, string output)
    {
        var htmlSanitizer = new HtmlSanitizer(new Config());
        var actual = HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) => htmlSanitizer.SanitizeHtml(ErrorBuilder.Null, ref reader, ref token, null));

        Assert.Equal(JsonDiff.NormalizeHtml(output), JsonDiff.NormalizeHtml(actual));
    }

    [Theory]
    [InlineData("", "666", "")]
    [InlineData("</a>", "666", "</a>")]
    [InlineData("<a href='hello'>", "666", "<a href='666'>")]
    [InlineData("<a href='hello'>", null, "<a href=''>")]
    [InlineData("<a href='hello'>", "~!@#$%^&*()<>?:,./][{}|", "<a href='~!@#$%^&amp;*()&lt;&gt;?:,./][{}|'>")]
    [InlineData("<A hrEf=''>", "666", "<A hrEf='666'>")]
    [InlineData("<a href = 'hello'>", "666", "<a href='666'>")]
    [InlineData("<a   target='_blank'   href='h'>", "666", "<a target='_blank' href='666'>")]
    [InlineData("<img src='a/b.png' />", "666", "<img src='666'/>")]
    [InlineData("<iMg sRc = 'a/b.png' />", "666", "<iMg sRc='666'/>")]
    [InlineData("<div><a href='hello'><img src='a/b.png' /></div>", "666", "<div><a href='666'><img src='666'/></div>")]
    [InlineData("<div><img src='a/b.png' /><a href='hello'></div>", "666", "<div><img src='666'/><a href='666'></div>")]
    public void TransformLinks(string input, string link, string output)
    {
        var actual = HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) => HtmlUtility.TransformLink(ref token, null, _ => link));

        Assert.Equal(output, actual);
    }

    [Theory]
    [InlineData("", "a", "a", "")]
    [InlineData("<a href='hello'>", "a", "a", "<a href='hello'>")]
    [InlineData("<xref href='hello'>", "a", "b", "<a href='a'>b</a>")]
    [InlineData("<xref uid='hello'>", "a", "b", "<a href='a'>b</a>")]
    [InlineData("<xref href='hello'>x</xref>", "a", "b", "<a href='a'>b</a>")]
    [InlineData("<xref href='hello' uid='hello'>", "a", "b", "<a href='a'>b</a>")]
    [InlineData(@"<xref href='hello' data-raw-html='@higher&amp;' data-raw-source='@lower'>", "", "", @"@higher&amp;")]
    [InlineData(@"<xref uid='hello' data-raw-html='@higher&amp;' data-raw-source='@lower'>", "", "", @"@higher&amp;")]
    [InlineData(@"<xref href='hello' data-raw-source='@lower&amp;'>", "", "", @"@lower&amp;")]
    [InlineData(@"<xref uid='hello' data-raw-source='@lower&amp;'>", "", "", @"@lower&amp;")]
    [InlineData(@"<xref href='a&amp;b' data-raw-source='@lower&amp;'>", "c&d", "", @"<a href='c&amp;d'></a>")]
    [InlineData(@"<xref uid='a&amp;b' data-raw-source='@lower&amp;'>", "c&d", "", @"<a href='c&amp;d'></a>")]
    public void TransformXrefs(string input, string xref, string display, string output)
    {
        var actual = HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) => HtmlUtility.TransformXref(
                ref reader, ref token, null, (href, uid, suppressXrefNotFound) => (xref, display)));

        Assert.Equal(output, actual);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("a b", 2)]
    [InlineData("a b ?!", 2)]
    [InlineData("中 文 ?!", 2)]
    [InlineData(@"<p>a</p>b", 2)]
    [InlineData(@"<p>a</p>b<p>c</p>", 3)]
    [InlineData(@"<p>日</p>本<p>語</p>", 3)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information ?!</h1></div></div>", 4)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1></div></div>", 4)]
    [InlineData(@"<div><div class=""content""><h1>Windows Forms 애플리케이션에서 데이터 흐름 사용</h1></div></div>", 17)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [InlineData(@"<div><title>Connect and TFS information</title><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 39)]
    [InlineData(@"<div><title>.NET 中的并行编程</title><div class=""content""><h1>.NET 中的并行编程</h1><p>Visual Studio 和 .NET Framework 提供了运行时、类库类型和诊断工具，从而增强了对并行编程的支持。 .NET Framework 4 中引入的这些功能简化了并行开发。</p></div></div>", 69)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the <a href=""http://www.msdn.com"">MSDN</a> and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    public static void CountWord(string input, long expectedCount)
    {
        var actualCount = 0L;
        HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                if (token.Type == HtmlTokenType.Text)
                {
                    actualCount += WordCount.CountWord(token.RawText.Span);
                }
            });

        Assert.Equal(expectedCount, actualCount);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("<h1 id='a'></h1>", "a")]
    [InlineData("<h1 id='a'></h1><h2 id='b'></h2>", "a, b")]
    [InlineData("<a id='a'></a>", "a")]
    [InlineData("<a name='a'></a>", "a")]
    public static void GetBookmarks(string input, string expected)
    {
        var bookmarks = new HashSet<string>();
        HtmlUtility.TransformHtml(
            input,
            (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) => HtmlUtility.GetBookmarks(ref token, bookmarks));

        Assert.Equal(expected, string.Join(", ", bookmarks));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("rétablir", "rétablir")]
    [InlineData("&<>\"'", "&amp;<>&quot;'")]
    public static void Encode(string input, string expected)
    {
        Assert.Equal(expected, HtmlUtility.Encode(input));
    }
}
