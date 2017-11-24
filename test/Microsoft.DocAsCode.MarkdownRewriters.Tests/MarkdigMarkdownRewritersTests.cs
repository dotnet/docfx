// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters.Tests
{
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdigMarkdownRewriters;

    using Xunit;

    public class MarkdigMarkdownRewritersTests
    {
        private DfmEngine _engine;

        public MarkdigMarkdownRewritersTests()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var builder = new DfmEngineBuilder(option);
            _engine = builder.CreateDfmEngine(new MarkdigMarkdownRenderer());
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Resloved_ShortcutXref()
        {
            var source = "@System.String";
            var expected = "@\"System.String\"\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Unresloved_ShortcutXref()
        {
            var source = "@outlook.com";
            var expected = "@outlook.com\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_AutoLinkXref()
        {
            var source = "<xref:system.string>";
            var expected = "<xref:system.string>\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_NormalizeVideo()
        {
            var source = @"> [!VIDEO https://channel9.msdn.com]
>
>
";
            var expected = @"> [!VIDEO https://channel9.msdn.com]

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_MailTo()
        {
            var source = "<Mailto:docs@microsoft.com>";
            var expected = "<docs@microsoft.com>\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        private string Rewrite(string source, string filePath)
        {
            return _engine.Markup(source, filePath);
        }
    }
}
