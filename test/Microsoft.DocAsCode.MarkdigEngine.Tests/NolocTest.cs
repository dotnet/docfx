namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class NolocTest
    {
        [Fact]
        [Trait("Related", "Noloc")]
        public void NolocTest_General()
        {
            // Case insensitive
            var source = "使用 ::: NoLoc Text=\"Find\" ::: 方法.";
            var expected = "<p>使用 Find 方法.</p>\n";

            TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        [Trait("Related", "Noloc")]
        public void NolocTest_Invalid()
        {
            // MultipleLines
            var source1 = @"::: noloc text=""I am crossing
a line"" :::";
            var expected1 = @"<p>::: noloc text=&quot;I am crossing
a line&quot; :::</p>
";
            TestUtility.AssertEqual(expected1, source1, TestUtility.MarkupWithoutSourceInfo);

            // Not exactly match
            var source2 = @":::noloc text=""test"":::";
            var expected2 = @"<p>:::noloc text=&quot;test&quot;:::</p>
";
            TestUtility.AssertEqual(expected2, source2, TestUtility.MarkupWithoutSourceInfo);

            // Markdown in noloc
            var source3 = @"::: noloc text=""*Hello*"" :::";
            var expected3 = @"<p>*Hello*</p>
";
            TestUtility.AssertEqual(expected3, source3, TestUtility.MarkupWithoutSourceInfo);
        }
    }
}
