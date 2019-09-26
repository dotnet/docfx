// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class ChromelessFormsTest
    {
        static public string LoggerPhase = "ChromelessForms";

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

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-form" });
        }

        [Fact]
        public void ChromelessFormsAttributeEndQuotationsRequired()
        {
            var content = @"::: form submitText=""something :::";

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-form" });
        }


        [Fact]
        public void ChromelessFormsAttributeValueRequired()
        {
            var content = @"::: form submitText :::";

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-form" });
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

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-form" });
        }

        [Fact]
        public void ChromelessFormsTestSubmitTextRequired()
        {
            var content = @"::: form action=""create-Resource"" :::";

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-form" });
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
}