namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class NolocTest
    {
        [Fact]
        [Trait("Related", "Noloc")]
        public void NolocTest_General()
        {
            // Normal syntax
            TestUtility.VerifyMarkup("使用 :::no-loc text=\"Find\"::: 方法.", "<p>使用 Find 方法.</p>");

            // Escape syntax
            TestUtility.VerifyMarkup(
                "使用 :::no-loc text=\"Find a \\\"Quotation\\\"\"::: 方法.",
                "<p>使用 Find a \"Quotation\" 方法.</p>\n");

            // Markdown in noloc
            TestUtility.VerifyMarkup(
                @":::no-loc text=""*Hello*"":::",
                @"<p>*Hello*</p>");
        }

        [Fact]
        [Trait("Related", "Noloc")]
        public void NolocTest_Invalid()
        {
            // MultipleLines
            TestUtility.VerifyMarkup(
                @":::no-loc text=""I am crossing\
a line"":::", 
                @"<p>:::no-loc text=&quot;I am crossing<br />a line&quot;:::</p>");

            // Spaces not exactly match
            TestUtility.VerifyMarkup(
                @"::: no-loc text=""test"" :::",
                @"<p>::: no-loc text=&quot;test&quot; :::</p>");

            // Case sensitive
            TestUtility.VerifyMarkup(
                @":::No-loc text=""test"":::", @"<p>:::No-loc text=&quot;test&quot;:::</p>");
        }
    }
}
