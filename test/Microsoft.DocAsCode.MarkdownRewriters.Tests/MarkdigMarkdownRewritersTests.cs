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
        public void TestMarkdigMarkdownRewriters_ShortcutXref()
        {
            var source = "@system.string";
            var expected = "@\"system.string\"\n\n";

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

        private string Rewrite(string source, string filePath)
        {
            return _engine.Markup(source, filePath);
        }
    }
}
