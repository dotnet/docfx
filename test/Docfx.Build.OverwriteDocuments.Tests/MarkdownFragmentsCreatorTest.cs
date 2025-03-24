// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Syntax;

namespace Docfx.Build.OverwriteDocuments.Tests;

[TestClass]
public class MarkdownFragmentsCreatorTest
{
    [TestMethod]
    public void BaseTest()
    {
        var markdown = File.ReadAllText("TestData/System.String.yml.md");
        var ast = Markdown.Parse(markdown);
        var model = new MarkdownFragmentsCreator().Create(ast).ToList();

        Assert.AreEqual(2, model.Count);
        Assert.AreEqual("System.String", model[0].Uid);
        Assert.IsNotNull(model[0].UidSource);
        Assert.AreEqual("author: rpetrusha\nms.author: ronpet\nmanager: wpickett", model[0].YamlCodeBlock.Replace("\r", ""));
        Assert.IsNotNull(model[0].YamlCodeBlockSource);
        Assert.AreEqual(4, model[0].Contents.Count);
        Assert.AreEqual("summary", model[0].Contents[0].PropertyName);
        Assert.IsNotNull(model[0].Contents[0].PropertyNameSource);
        Assert.ContainsSingle(model[0].Contents[0].PropertyValue);
        Assert.IsInstanceOfType<ParagraphBlock>(model[0].Contents[0].PropertyValue[0]);
        Assert.AreEqual("remarks", model[0].Contents[1].PropertyName);
        Assert.AreEqual(6, model[0].Contents[1].PropertyValue.Count);
        Assert.AreEqual("System.String.#ctor(System.Char*)", model[1].Uid);
    }

    [TestMethod]
    public void MissingStartingH1CodeHeadingShouldFail()
    {
        var markdown = @"## `summary`
markdown content
## `description`
markdown content
";
        var ast = Markdown.Parse(markdown);

        var ex = Assert.Throws<MarkdownFragmentsException>(() => new MarkdownFragmentsCreator().Create(ast).ToList());
        Assert.AreEqual("Expect L1InlineCodeHeading", ex.Message);
        Assert.AreEqual(0, ex.Position);
    }

    [TestMethod]
    public void MarkdownContentAfterL1CodeHeadingShouldFail()
    {
        var markdown = @"# `Lesson_1`

## `Lesson_1_1`

markdown content

# `Lesson_2`

markdown content
";
        var ast = Markdown.Parse(markdown);

        var ex = Assert.Throws<MarkdownFragmentsException>(() => new MarkdownFragmentsCreator().Create(ast).ToList());
        Assert.AreEqual("Expect L1InlineCodeHeading", ex.Message);
        Assert.AreEqual(8, ex.Position);
    }

    [TestMethod]
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
        var model = new MarkdownFragmentsCreator().Create(ast).ToList();

        Assert.IsNull(model[0].YamlCodeBlock);
        Assert.IsNull(model[0].YamlCodeBlockSource);
        Assert.IsInstanceOfType<FencedCodeBlock>(model[0].Contents[0].PropertyValue[1]);
    }
}
