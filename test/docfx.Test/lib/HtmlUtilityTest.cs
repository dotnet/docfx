// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlAgilityPack;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class HtmlUtilityTest
    {
        [Theory]
        [InlineData("<a href='a.md' />", "<a href='a.md' data-linktype='relative-path' />")]
        [InlineData("<a href='#aA' />", "<a href='#aA' data-linktype='self-bookmark' />")]
        [InlineData("<a href='/a' />", "<a href='/zh-cn/a' data-linktype='absolute-path' />")]
        [InlineData("<a href='/Alink#fraGMENT' />", "<a href='/zh-cn/alink#fraGMENT' data-linktype='absolute-path' />")]
        [InlineData("<a href='/Alink?quERY' />", "<a href='/zh-cn/alink?quERY' data-linktype='absolute-path' />")]
        [InlineData("<a href='/a#x' />", "<a href='/zh-cn/a#x' data-linktype='absolute-path' />")]
        [InlineData("<a href='/de-de/a' />", "<a href='/de-de/a' data-linktype='absolute-path' />")]
        [InlineData("<a href='http://abc' />", "<a href='http://abc' data-linktype='external' />")]
        [InlineData("<a href='https://abc' />", "<a href='https://abc' data-linktype='external' />")]
        public void AddLinkType(string input, string output)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(input);
            HtmlUtility.AddLinkType(doc.DocumentNode, "zh-cn");

            Assert.Equal(
                TestHelper.NormalizeHtml(output),
                TestHelper.NormalizeHtml(doc.DocumentNode.OuterHtml));
        }
    }
}
