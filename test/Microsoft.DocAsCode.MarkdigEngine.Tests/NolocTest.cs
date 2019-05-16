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
            var source1 = "使用 :::no-loc text=\"Find\"::: 方法.";
            var expected1 = "<p>使用 Find 方法.</p>\n";
            TestUtility.AssertEqual(expected1, source1, TestUtility.MarkupWithoutSourceInfo);

            // Escape syntax
            var source2 = "使用 :::no-loc text=\"Find a \\\"Quotation\\\"\"::: 方法.";
            var expected2 = "<p>使用 Find a \"Quotation\" 方法.</p>\n";
            TestUtility.AssertEqual(expected2, source2, TestUtility.MarkupWithoutSourceInfo);

            // Markdown in noloc
            var source3 = @":::no-loc text=""*Hello*"":::";
            var expected3 = @"<p>*Hello*</p>
";
            TestUtility.AssertEqual(expected3, source3, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        [Trait("Related", "Noloc")]
        public void NolocTest_Invalid()
        {
            // MultipleLines
            var source1 = @":::no-loc text=""I am crossing\
a line"":::";
            var expected1 = @"<p>:::no-loc text=&quot;I am crossing<br />
a line&quot;:::</p>
";
            TestUtility.AssertEqual(expected1, source1, TestUtility.MarkupWithoutSourceInfo);

            // Spaces not exactly match
            var source2 = @"::: no-loc text=""test"" :::";
            var expected2 = @"<p>::: no-loc text=&quot;test&quot; :::</p>
";
            TestUtility.AssertEqual(expected2, source2, TestUtility.MarkupWithoutSourceInfo);

            // Case sensitive
            var source3 = @":::No-loc text=""test"":::";
            var expected3 = @"<p>:::No-loc text=&quot;test&quot;:::</p>
";
            TestUtility.AssertEqual(expected3, source3, TestUtility.MarkupWithoutSourceInfo);
        }
    }
}
