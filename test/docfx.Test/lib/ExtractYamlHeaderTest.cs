// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public class ExtractYamlHeaderTest
{
    /// <summary>
    /// https://github.com/lunet-io/markdig/blob/master/src/Markdig.Tests/Specs/YamlSpecs.md
    /// </summary>
    [Theory]
    [InlineData(
@"---
this: is a frontmatter
---", "{'this':'is a frontmatter'}")]
    [InlineData(
@"This is a text1
---
this: is a frontmatter
---", "{}")]
    [InlineData(
@"---
this: is a frontmatter

...", "{'this':'is a frontmatter'}")]
    [InlineData(
@"---
this: is a frontmatter

....", "{}")]
    [InlineData(
@"---   
this: is a frontmatter
...", "{'this':'is a frontmatter'}")]
    [InlineData(
@"---
this: is a frontmatter
...   ", "{'this':'is a frontmatter'}")]
    [InlineData(
@"----
this: is a frontmatter
---", "{}")]
    [InlineData(
@"some text
---
this: is a frontmatter
---", "{}")]
    [InlineData(
@"
---
this: is a frontmatter
---", "{}")]
    public void TestExtract(string content, string expectedMetadata)
    {
        using var reader = new StringReader(content);
        var errors = new ErrorList();
        var metadata = ExtractYamlHeader.Extract(errors, reader, new FilePath(""));
        Assert.Empty(errors.ToArray());
        Assert.Equal(expectedMetadata.Replace('\'', '"'), JsonUtility.Serialize(metadata));
    }

    [Theory]
    [InlineData(
@"---
hello
...", "yaml-header-not-object", "Expect yaml header to be an object, but got a scalar.")]
    [InlineData(
@"---
- 1
- 2
...", "yaml-header-not-object", "Expect yaml header to be an object, but got an array.")]
    public void TestNotJObject(string content, string expectedErrorCode, string expectedErrorMessage)
    {
        using var reader = new StringReader(content);
        var errors = new ErrorList();
        var metadata = ExtractYamlHeader.Extract(errors, reader, new FilePath(""));
        Assert.Collection(errors.ToArray(), error =>
        {
            Assert.Equal(expectedErrorCode, error.Code);
            Assert.Equal(expectedErrorMessage, error.Message);
        });
    }
}
