// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlAgilityPack;
using Xunit;

namespace Microsoft.Docs.Build.build
{
    public static class MetadataTest
    {
        [InlineData("", 0)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information ?!</h1></div></div>", 4)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1></div></div>", 4)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
        [InlineData(@"<div><title>Connect and TFS information</title><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
        [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the <a href=""http://www.msdn.com"">MSDN</a> and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
        [Theory]
        public static void TestWordCounter(string html, long expectedCount)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            Assert.Equal(expectedCount, HtmlUtility.CountWord(document.DocumentNode));
        }
    }
}
