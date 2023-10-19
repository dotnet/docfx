// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using static Docfx.Build.HtmlTemplate;

namespace Docfx.Build.Engine.Tests;

public class HtmlTemplateTest
{
    [Fact]
    public void HtmlTemplate_HtmlTest()
    {
        Assert.Equal("ab", Html($"a{null}b").ToString());
        Assert.Equal("a1b", Html($"a{1}b").ToString());
        Assert.Equal("a&lt;br/&gt;b", Html($"a{"<br/>"}b").ToString());
        Assert.Equal("a<br/>b", Html($"a{UnsafeHtml("<br/>")}b").ToString());
        Assert.Equal("a<p>2</p>b", Html($"a{Html($"<p>{2}</p>")}b").ToString());
        Assert.Equal("<ul><li>0</li><li>1</li></ul>", Html($"<ul>{new[] { 0, 1 }.Select(i => Html($"<li>{i}</li>"))}</ul>").ToString());
    }

    [Fact]
    public void HtmlTemplate_HtmlAttributesTest()
    {
        Assert.Equal("<h1></h1>", Html($"<h1 id=\"{false}\"></h1>").ToString());
        Assert.Equal("<h1></h1>", Html($"<h1 id=\"{null}\"></h1>").ToString());
        Assert.Equal("<h1></h1>", Html($"<h1 id='{""}'></h1>").ToString());
        Assert.Equal("<h1 id='a'></h1>", Html($"<h1 id='{"a"}'></h1>").ToString());
        Assert.Equal("<h1 id=\"a\"></h1>", Html($"<h1 id=\"{"a"}\"></h1>").ToString());
        Assert.Equal("<h1 id></h1>", Html($"<h1 id=\"{true}\"></h1>").ToString());
    }
}
