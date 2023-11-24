﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Build.ConceptualDocuments.Tests;

[Trait("Related", "Conceptual")]
public class WordCounterUnitTest
{
    [InlineData("", 0)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information ?!</h1></div></div>", 4)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1></div></div>", 4)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [InlineData(@"<div><title>Connect and TFS information</title><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [InlineData(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the <a href=""http://www.msdn.com"">MSDN</a> and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [Theory]
    public void TestWordCounter(string html, long expectedCount)
    {
        long wordCount = WordCounter.CountWord(html);
        Assert.Equal(expectedCount, wordCount);
    }
}
