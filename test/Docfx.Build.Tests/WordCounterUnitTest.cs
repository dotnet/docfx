// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.ConceptualDocuments.Tests;

[TestProperty("Related", "Conceptual")]
[TestClass]
public class WordCounterUnitTest
{
    [DataRow("", 0)]
    [DataRow(@"<div><div class=""content""><h1>Connect and TFS information ?!</h1></div></div>", 4)]
    [DataRow(@"<div><div class=""content""><h1>Connect and TFS information</h1></div></div>", 4)]
    [DataRow(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [DataRow(@"<div><title>Connect and TFS information</title><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the MSDN and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [DataRow(@"<div><div class=""content""><h1>Connect and TFS information</h1><p>Open Publishing is being developed by the Visual Studio China team. The team owns the <a href=""http://www.msdn.com"">MSDN</a> and Technet platforms, as well as CAPS authoring tool, which is the replacement of DxStudio.</p></div></div>", 35)]
    [TestMethod]
    public void TestWordCounter(string html, long expectedCount)
    {
        long wordCount = WordCounter.CountWord(html);
        Assert.AreEqual(expectedCount, wordCount);
    }
}
