// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Glob;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    public class FileMetadataHelperTest
    {
        [Fact]
        public void TestGetChangedGlobs_AllTheSame()
        {
            var baseDir = "inputFolder";
            var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                    new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });

            var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                    new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });


            var actual = FileMetadataHelper.GetChangedGlobs(left, right);
            Assert.True(actual != null);
            Assert.True(actual.Count() == 0);
        }

        [Fact]
        public void TestGetChangedGlobs_AllChanged_With_BaseDir()
        {
            var baseDir = "inputFolder";
            var patternsA = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
            var patternsB = new string[] { "*mdB", "*.mB", "abcB", "/[]\\*.csB", "*/*.csB", "**B" };
            var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patternsA[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patternsA[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patternsA[2]), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher(patternsA[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher(patternsA[4]), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher(patternsA[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });

            var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patternsB[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patternsB[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patternsB[2]), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher(patternsB[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher(patternsB[4]), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher(patternsB[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });

            var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
            Assert.True(actualResults != null);
            Assert.True(actualResults.Count() == 12);
            var patterns = patternsA.Concat(patternsB).ToList();
            for (var index = 0; index < patterns.Count(); index++)
            {
                Assert.Equal(patterns[index], actualResults[index].Raw);
            }
        }

        [Fact]
        public void TestGetChangedGlobs_AllChanged()
        {
            var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
            var left = new FileMetadata("inputFolder1", new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher(patterns[4]), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher(patterns[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });

            var right = new FileMetadata("inputFolder2", new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher(patterns[3]), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher(patterns[4]), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher(patterns[5]), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            });

            var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
            Assert.True(actualResults != null);
            Assert.True(actualResults.Count() == 6);
            for (var index = 0; index < patterns.Length; index++)
            {
                Assert.Equal(patterns[index], actualResults[index].Raw);
            }
        }

        [Fact]
        public void TestGetChangedGlobs_CrossGlobsChanged()
        {
            var baseDir = "inputFolder";
            var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
            var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
                )
            });

            var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string")
                )
            });

            var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
            Assert.True(actualResults != null);
            Assert.True(actualResults.Count() == 2);
            Assert.Equal(patterns[0], actualResults[0].Raw);
            Assert.Equal(patterns[2], actualResults[1].Raw);
        }

        [Fact]
        public void TestGetChangedGlobs_ReduceGlobsChanged()
        {
            var baseDir = "inputFolder";
            var patterns = new string[] { "*md", "*.m", "abc", "/[]\\*.cs", "*/*.cs", "**" };
            var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
                )
            });

            var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L)
                )
            });

            var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
            Assert.True(actualResults != null);
            Assert.True(actualResults.Count() == 1);
            Assert.Equal(patterns[1], actualResults[0].Raw);
        }

        [Fact]
        public void TestGetChangedGlobs_IncreaseGlobsChanged()
        {
            var baseDir = "inputFolder";
            var patterns = new string[] { "*md", "*.m", "abc" };
            var left = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true)
                )
            });

            var right = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher(patterns[0]), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher(patterns[1]), "meta", true),
                    new FileMetadataItem(new GlobMatcher(patterns[2]), "meta", "string")
                )
            });

            var actualResults = FileMetadataHelper.GetChangedGlobs(left, right).ToList();
            Assert.True(actualResults != null);
            Assert.True(actualResults.Count() == 1);
            Assert.Equal(patterns[2], actualResults[0].Raw);
        }
    }
}
