// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class MarkdigUtilityTest
    {
        private static MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().Build();

        [Theory]
        [InlineData("abc", true)]
        [InlineData("# ABC", true)]
        [InlineData("[link](https://github.com)", true)]
        [InlineData("![image](image.png)", true)]
        [InlineData("<img src=\"imag.png\"/>", true)]
        [InlineData("<img src=\"imag.png\">abc</img>", true)]
        [InlineData("<a href=\"www.microsoft.com\"><img src=\"imag.png\"/></a>", true)]
        [InlineData("<a>test</a>", true)]
        [InlineData("`code`", true)]

        [InlineData("<!--comments-->", false)]
        [InlineData("    ", false)]
        [InlineData("  \n  \n  ", false)]
        [InlineData("  \n\n  \n\n  ", false)]
        [InlineData("  \n <!--comments--> \n  ", false)]
        [InlineData("[](https://github.com)", false)]
        [InlineData("<a></a>", false)]
        [InlineData("<a/>", false)]
        [InlineData("<a href=\"https://www.microsoft.com\"></a>", false)]
        [InlineData("<a href=\"https://www.microsoft.com\"/>", false)]
        public static void IsVisibleTest(string markdownContent, bool expectedVisible)
        {
            var markdownDoucment = Markdown.Parse(markdownContent, _markdownPipeline);

            Assert.Equal(expectedVisible, MarkdigUtility.IsVisible(markdownDoucment));
        }
    }
}
