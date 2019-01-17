// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Glob;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    public class JsonConverterTest
    {
        [Fact]
        public void TestFileMetadataConverterCouldSerializeAndDeserialize()
        {
            var settings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new FileMetadataConverter() } };
            var baseDir = "inputFolder";
            var raw = new FileMetadata(baseDir, new Dictionary<string, ImmutableArray<FileMetadataItem>>
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
            var serialized = JsonConvert.SerializeObject(raw, settings);
            var expected = "{'baseDir':'inputFolder','dict':{'meta':[{'glob':'*.md','key':'meta','value':1},{'glob':'*.m','key':'meta','value':true},{'glob':'abc','key':'meta','value':'string'},{'glob':'/[]\\\\*.cs','key':'meta','value':{'key':'2'}},{'glob':'*/*.cs','key':'meta','value':['1','2']},{'glob':'**','key':'meta','value':{'key':['1','2']}}]}}".Replace('\'', '\"');
            Assert.Equal(expected, serialized);

            var actual = JsonConvert.DeserializeObject<FileMetadata>(serialized, settings);

            Assert.Equal(baseDir, actual.BaseDir);

        }

        [Fact]
        public void TestSerializeFileMetadataIgnoreBaseDir()
        {
            var settings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new FileMetadataConverter(true) } };
            var metadata = new Dictionary<string, ImmutableArray<FileMetadataItem>>
            {
                ["meta"] = ImmutableArray.Create(
                    new FileMetadataItem(new GlobMatcher("*.md"), "meta", 1L),
                    new FileMetadataItem(new GlobMatcher("*.m"), "meta", true),
                    new FileMetadataItem(new GlobMatcher("abc"), "meta", "string"),
                    new FileMetadataItem(new GlobMatcher("/[]\\*.cs"), "meta", new Dictionary<string, object> { ["key"] = "2" }),
                    new FileMetadataItem(new GlobMatcher("*/*.cs"), "meta", new object[] { "1", "2" }),
                    new FileMetadataItem(new GlobMatcher("**"), "meta", new Dictionary<string, object> { ["key"] = new object[] { "1", "2" } })
                )
            };
            var fileMetadata1 = new FileMetadata("baseDir1", metadata);
            var fileMetadata2 = new FileMetadata("baseDir2", metadata);

            var str1 = JsonConvert.SerializeObject(fileMetadata1, settings);
            var str2 = JsonConvert.SerializeObject(fileMetadata2, settings);
            var deserialized1 = JsonConvert.DeserializeObject<FileMetadata>(str1, settings);
            var deserialized2 = JsonConvert.DeserializeObject<FileMetadata>(str2, settings);

            Assert.Equal(str1, str2);
            CompareFileMetadataItems(deserialized1, deserialized2);
        }

        private void CompareFileMetadataItems(FileMetadata raw, FileMetadata actual)
        {
            Assert.Equal(raw.Count, actual.Count);
            foreach (var pair in raw)
            {
                Assert.True(actual.TryGetValue(pair.Key, out var array));
                Assert.Equal(pair.Value.Length, array.Length);
                for (var i = 0; i < array.Length; i++)
                {
                    Assert.Equal(pair.Value[i].Glob.Raw, array[i].Glob.Raw);
                    Assert.Equal(pair.Value[i].Key, array[i].Key);
                    Assert.Equal(pair.Value[i].Value, array[i].Value);
                }
            }
        }
    }
}
