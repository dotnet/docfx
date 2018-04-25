using Xunit;

namespace Microsoft.Docs.Build
{
    public static class HrefUtilityTest
    {
        [Theory]
        [InlineData("", "", "", "")]
        [InlineData("a", "a", "", "")]
        [InlineData("a#b", "a", "#b", "")]
        [InlineData("a#b<", "a", "#b<", "")]
        [InlineData("a?c", "a", "", "?c")]
        [InlineData("a?b#c", "a", "#c", "?b")]
        public static void SplitHref(string href, string path, string fragment, string query)
        {
            var (apath, afragment, aquery) = HrefUtility.SplitHref(href);

            Assert.Equal(path, apath);
            Assert.Equal(fragment, afragment);
            Assert.Equal(query, aquery);
        }
    }
}
