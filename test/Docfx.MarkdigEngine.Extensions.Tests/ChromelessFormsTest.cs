// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.MarkdigEngine.Tests;

[TestClass]
public class ChromelessFormsTest
{
    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public void ChromelessFormsAttributeStartQuotationsRequired()
    {
        var content = @"::: form submitText=something"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [TestMethod]
    public void ChromelessFormsAttributeEndQuotationsRequired()
    {
        var content = @"::: form submitText=""something :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [TestMethod]
    public void ChromelessFormsAttributeValueRequired()
    {
        var content = "::: form submitText :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [TestMethod]
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

    [TestMethod]
    public void ChromelessFormsTestActionRequired()
    {
        var content = @"::: form submitText=""Do it"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [TestMethod]
    public void ChromelessFormsTestSubmitTextRequired()
    {
        var content = @"::: form action=""create-Resource"" :::";

        TestUtility.VerifyMarkup(content, null, ["invalid-form"]);
    }

    [TestMethod]
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
