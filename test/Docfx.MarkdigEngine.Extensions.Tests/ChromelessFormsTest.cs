// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class ChromelessFormsTest
{
    [Fact]
    public void ChromelessFormsTestWithoutModel()
    {
        var content = @"::: form action=""create-resource"" submitText=""Create"" :::";
        var expected = @"<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
";

        TestUtility.VerifyMarkup(content, expected);
    }

    [Fact]
    public void ChromelessFormsTestWithModel()
    {
        var content = @"::: form model=""./devsandbox/ChromelessFormsTest.md"" action=""create-resource"" submitText=""Do it"" :::";
        var expected = @"<form class=""chromeless-form"" data-model=""./devsandbox/ChromelessFormsTest.md"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Do it</button>
</form>
";

        TestUtility.VerifyMarkup(content, expected);
    }

    [Fact]
    public void ChromelessFormsAttributeStartQuotationsRequired()
    {
        var content = @"::: form submitText=something"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [Fact]
    public void ChromelessFormsAttributeEndQuotationsRequired()
    {
        var content = @"::: form submitText=""something :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [Fact]
    public void ChromelessFormsAttributeValueRequired()
    {
        var content = "::: form submitText :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [Fact]
    public void ChromelessFormsAttributeValueSingleQuote()
    {
        var content = @"::: form submitText=""<script> >.< </script>"" action=""create-Resource"" :::";
        var expected = @"<form class=""chromeless-form"" data-action=""create-Resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">&lt;script&gt; &gt;.&lt; &lt;/script&gt;</button>
</form>
";
        TestUtility.VerifyMarkup(content, expected);
    }

    [Fact]
    public void ChromelessFormsTestActionRequired()
    {
        var content = @"::: form submitText=""Do it"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [Fact]
    public void ChromelessFormsTestSubmitTextRequired()
    {
        var content = @"::: form action=""create-Resource"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [Fact]
    public void ChromelessFormsTestMultipleForms()
    {
        var content = @"
::: form action=""create-Resource"" submitText=""Create""  :::

::: form action=""update-Resource"" submitText=""Update"" :::
";
        var expected = @"<form class=""chromeless-form"" data-action=""create-Resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
<form class=""chromeless-form"" data-action=""update-Resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Update</button>
</form>
";

        TestUtility.VerifyMarkup(content, expected);
    }
}
