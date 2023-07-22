// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class TripleColonTest
{
    [Fact]
    public void TripleColonTestGeneral()
    {
        var source = @"::: zone pivot=""windows""
    hello
::: zone-end
";
        var expected = @"<div class=""zone has-pivot"" data-pivot=""windows"">
<pre><code>hello
</code></pre>
</div>
";
        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void TripleColonTestSelfClosing()
    {
        var source = @"::: zone target=""chromeless""
::: form action=""create-resource"" submitText=""Create"" :::
::: zone-end
";

        var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
</div>
";
        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void TripleColonTestBlockClosed()
    {
        var source = @"::: zone target=""chromeless""
::: form action=""create-resource"" submitText=""Create"" :::
::: zone-end
";

        var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
</div>
";
        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void TripleColonWithInMonikerTestBlockClosed()
    {
        var source = new StringBuilder()
            .AppendLine("::: moniker range=\"chromeless\"")
            .AppendLine("::: zone target=\"docs\"")
            .AppendLine("## Alt text")
            .AppendLine("::: zone-end")
            .AppendLine("::: moniker-end")
            .ToString();

        var expected = @"<div range=""chromeless"">
<div class=""zone has-target"" data-target=""docs"">
<h2 id=""alt-text"">Alt text</h2>
</div>
</div>";

        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void TripleColonWithInMonikerTestBlockUnClosed()
    {
        var source = new StringBuilder()
            .AppendLine("::: moniker range=\"chromeless\"")
            .AppendLine("::: zone target=\"docs\"")
            .AppendLine("## Alt text")
            .AppendLine("::: moniker-end")
            .ToString();

        var expected = @"<div range=""chromeless"">
<div class=""zone has-target"" data-target=""docs"">
<h2 id=""alt-text"">Alt text</h2>
</div>
</div>
";

        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void Issue8999()
    {
        TestUtility.VerifyMarkup(
            """
            **CONTACT POINT** Use the :::image type="icon" source="../images/copy.png"::: button on the right side of the screen to copy the top value, **CONTACT POINT**
            """,
            """
            <p><strong>CONTACT POINT</strong> Use the <img src="../images/copy.png" role="presentation"> button on the right side of the screen to copy the top value, <strong>CONTACT POINT</strong></p>
            """);
    }
}
