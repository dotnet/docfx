// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments.Tests
{
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.OverwriteDocuments;

    using Markdig;
    using Markdig.Syntax;
    using Xunit;

    [Trait("Owner", "renzeyu")]
    [Trait("EntityType", nameof(MarkdownFragmentsCreater))]
    public class MarkdownFragmentsCreaterTest
    {
        [Fact]
        public void BaseTest()
        {
            var markdown = File.ReadAllText("TestData/System.String.yml.md");
            var ast = Markdown.Parse(markdown);
            var model = new MarkdownFragmentsCreater().Create(ast).ToList();

            Assert.Equal(2, model.Count);
            Assert.Equal("System.String", model[0].Uid);
            Assert.NotNull(model[0].UidSource);
            Assert.Equal("author: rpetrusha\nms.author: ronpet\nmanager: wpickett", model[0].YamlCodeBlock);
            Assert.NotNull(model[0].YamlCodeBlockSource);
            Assert.Equal(4, model[0].Contents.Count);
            Assert.Equal("summary", model[0].Contents[0].PropertyName);
            Assert.NotNull(model[0].Contents[0].PropertyNameSource);
            Assert.Single(model[0].Contents[0].PropertyValue);
            Assert.IsType<ParagraphBlock>(model[0].Contents[0].PropertyValue[0]);
            Assert.Equal("remarks", model[0].Contents[1].PropertyName);
            Assert.Equal(6, model[0].Contents[1].PropertyValue.Count);
            Assert.Equal("System.String.#ctor(System.Char*)", model[1].Uid);
        }

        [Fact]
        public void MissingStartingH1CodeHeadingShouldFail()
        {
            var markdown = @"## `summary`
markdown content
## `description`
markdown content
";
            var ast = Markdown.Parse(markdown);

            var ex = Assert.Throws<MarkdownFragmentsException>(() => new MarkdownFragmentsCreater().Create(ast).ToList());
            Assert.Equal("Expect L1InlineCodeHeading", ex.Message);
            Assert.Equal(0, ex.Position);
        }

        [Fact]
        public void MarkdownContentAfterL1CodeHeadingShouldFail()
        {
            var markdown = @"# `Lesson_1`

## `Lesson_1_1`

markdown content

# `Lesson_2`

markdown content
";
            var ast = Markdown.Parse(markdown);

            var ex = Assert.Throws<MarkdownFragmentsException>(() => new MarkdownFragmentsCreater().Create(ast).ToList());
            Assert.Equal("Expect L1InlineCodeHeading", ex.Message);
            Assert.Equal(8, ex.Position);
        }

        [Fact]
        public void YamlCodeBlockShouldBeNextToL1CodeHeading()
        {
            var markdown = @"# `YAML`

## `Introduction`

This is just a normal yaml fences block:
``` yaml
a: b
c: d
```
";
            var ast = Markdown.Parse(markdown);
            var model = new MarkdownFragmentsCreater().Create(ast).ToList();

            Assert.Null(model[0].YamlCodeBlock);
            Assert.Null(model[0].YamlCodeBlockSource);
            Assert.IsType<FencedCodeBlock>(model[0].Contents[0].PropertyValue[1]);
        }
    }
}
