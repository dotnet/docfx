// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;
    using System.Linq;

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
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, content, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        public void ChromelessFormsTestWithModel()
        {
            var content = @"::: form model=""./devsandbox/ChromelessFormsTest.md"" action=""create-resource"" submitText=""Do it"" :::";
            var expected = @"<form class=""chromeless-form"" data-model=""./devsandbox/ChromelessFormsTest.md"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Do it</button>
</form>
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, content, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        public void ChromelessFormsAttributeStartQuotationsRequired()
        {
            var content = @"::: form submitText=something"" :::";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            // Listener should have an error message and not output.
            Assert.NotEmpty(listener.Items.Where(x => x.Code == "invalid-form"));
        }

        [Fact]
        public void ChromelessFormsAttributeEndQuotationsRequired()
        {
            var content = @"::: form submitText=""something :::";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            // Listener should have an error message and not output.
            Assert.NotEmpty(listener.Items.Where(x => x.Code == "invalid-form"));
        }


        [Fact]
        public void ChromelessFormsAttributeValueRequired()
        {
            var content = @"::: form submitText :::";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            // Listener should have an error message and not output.
            Assert.NotEmpty(listener.Items.Where(x => x.Code == "invalid-form"));
        }

        [Fact]
        public void ChromelessFormsAttributeValueSingleQuote()
        {
            var content = @"::: form submitText=""<script> >.< </script>"" action=""create-Resource"" :::";
            var expected = @"<form class=""chromeless-form"" data-action=""create-Resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">&lt;script&gt; &gt;.&lt; &lt;/script&gt;</button>
</form>
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, content, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        public void ChromelessFormsTestActionRequired()
        {
            var content = @"::: form submitText=""Do it"" :::";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            // Listener should have an error message and not output.
            Assert.NotEmpty(listener.Items.Where(x => x.Code == "invalid-form"));
        }

        [Fact]
        public void ChromelessFormsTestSubmitTextRequired()
        {
            var content = @"::: form action=""create-Resource"" :::";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.MarkupWithoutSourceInfo(content);
            }
            Logger.UnregisterListener(listener);

            // Listener should have an error message and not output.
            Assert.NotEmpty(listener.Items.Where(x => x.Code == "invalid-form"));
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
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, content, TestUtility.MarkupWithoutSourceInfo);
        }
    }
}