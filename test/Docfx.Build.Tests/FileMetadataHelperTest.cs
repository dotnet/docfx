// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Glob;

namespace Docfx.Build.Engine.Tests;

[TestClass]
public class FileMetadataHelperTest
{
    [TestMethod]
    public void TestGetChangedGlobs_AllTheSame()
    {
        var baseDir = "inputFolder";
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var actual = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actual);
        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public void TestGetChangedGlobs_AllChanged_With_DifferentBaseDir()
    {
        var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
        var left = new FileMetadata("inputFolder1", new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string"),
                new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[4]), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var right = new FileMetadata("inputFolder2", new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string"),
                new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[4]), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.IsEmpty(actualResults);
    }

    [TestMethod]
    public void TestGetChangedGlobs_AllChanged_With_DifferentPattern()
    {
        var baseDir = "inputFolder";
        var patternsA = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
        var patternsB = new string[] { "*mdB", "*.mB", "abcB", "/[]\\*.csB", "*/*.csB", "**B" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher(patternsA[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patternsA[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patternsA[2]), "meta", "string"),
                new FileMetadataItem(new GlobMatcher(patternsA[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patternsA[4]), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher(patternsA[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher(patternsB[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patternsB[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patternsB[2]), "meta", "string"),
                new FileMetadataItem(new GlobMatcher(patternsB[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patternsB[4]), "meta", new object[] { "1", "2" }),
                new FileMetadataItem(new GlobMatcher(patternsB[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.AreEqual(12, actualResults.Count);
        var patterns = patternsA.Concat(patternsB).ToList();
        for (var index = 0; index < patterns.Count; index++)
        {
            Assert.AreEqual(patterns[index], actualResults[index].Raw);
        }
    }

    [TestMethod]
    public void TestGetChangedGlobs_CrossGlobsChanged()
    {
        var baseDir = "inputFolder";
        var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
,
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string")
,
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.AreEqual(2, actualResults.Count);
        Assert.AreEqual(patterns[0], actualResults[0].Raw);
        Assert.AreEqual(patterns[2], actualResults[1].Raw);
    }

    [TestMethod]
    public void TestGetChangedGlobs_ReduceGlobsChanged()
    {
        var baseDir = "inputFolder";
        var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
,
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L)]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.ContainsSingle(actualResults);
        Assert.AreEqual(patterns[1], actualResults[0].Raw);
    }

    [TestMethod]
    public void TestGetChangedGlobs_IncreaseGlobsChanged()
    {
        var baseDir = "inputFolder";
        var patterns = new string[] { "*md", "*.m", "abc" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
,
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string")
,
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.ContainsSingle(actualResults);
        Assert.AreEqual(patterns[2], actualResults[0].Raw);
    }

    [TestMethod]
    public void TestGetChangedGlobs_Changed()
    {
        var baseDir = "inputFolder";
        var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =

            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
,
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] = [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string"),
                new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                new FileMetadataItem(new GlobMatcher(patterns[4]), "meta", new object[] { "1", "2" })
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.AreEqual(3, actualResults.Count);
        Assert.AreEqual(patterns[5], actualResults[0].Raw);
        Assert.AreEqual(patterns[2], actualResults[1].Raw);
        Assert.AreEqual(patterns[4], actualResults[2].Raw);
    }

    [TestMethod]
    public void TestGetChangedGlobs_Changed_Reverse()
    {
        var baseDir = "inputFolder";
        var patterns = new string[] { "*md", "*.m" };
        var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =
            [
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
            ]
        });

        var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
        {
            ["meta"] =
            [
                new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
            ]
        });

        var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
        Assert.IsNotNull(actualResults);
        Assert.ContainsSingle(actualResults);
        Assert.AreEqual(patterns[1], actualResults[0].Raw);
    }
}
