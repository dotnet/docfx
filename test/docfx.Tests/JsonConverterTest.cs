// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class JsonConverterTest
    {
        [Fact]
        [Trait("Related", "docfx")]
        public void TestFileMetadataPairsConverterCouldSerializeAndDeserialize()
        {
            FileMetadataPairs item = new FileMetadataPairs(
                new List<FileMetadataPairsItem>
                {
                    new FileMetadataPairsItem("*.md", 1L),
                    new FileMetadataPairsItem("*.m", true),
                    new FileMetadataPairsItem("abc", "string"),
                    new FileMetadataPairsItem("/[]\\*.cs", new Dictionary<string, object>{ ["key"] = "2" }),
                    new FileMetadataPairsItem("*/*.cs", new object[] { "1", "2" }),
                    new FileMetadataPairsItem("**", new Dictionary<string, object>{ ["key"] = new object[] {"1", "2" } }),
                });
            var result = JsonUtility.Serialize(item);
            Assert.Equal("{\"*.md\":1,\"*.m\":true,\"abc\":\"string\",\"/[]\\\\*.cs\":{\"key\":\"2\"},\"*/*.cs\":[\"1\",\"2\"],\"**\":{\"key\":[\"1\",\"2\"]}}", result);
            using (var reader = new StringReader(result))
            {
                var pairs = JsonUtility.Deserialize<FileMetadataPairs>(reader);
                Assert.Equal(item.Count, pairs.Count);
                for (int i = 0; i < pairs.Count; i++)
                {
                    Assert.Equal(item[i].Glob.Raw, pairs[i].Glob.Raw);
                    var parsedValue = pairs[i].Value;
                    Assert.Equal(item[i].Value, parsedValue);
                }
            }
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestFileMappingItemCwdInputShouldWork()
        {
            var input = "{\"files\":[\"file1\"],\"cwd\":\"folder1\"}";
            using (var sr = new StringReader(input))
            {
                var result = JsonUtility.Deserialize<FileMappingItem>(sr);
                Assert.Equal("folder1", result.SourceFolder);
            }
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestFileMappingItemSrcInputShouldWork()
        {
            var input = "{\"files\":[\"file1\"],\"src\":\"folder1\"}";
            using(var sr = new StringReader(input))
            {
                var result = JsonUtility.Deserialize<FileMappingItem>(sr);
                Assert.Equal("folder1", result.SourceFolder);
            }
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestFileMappingItemOutputShouldContainSrcOnly()
        {
            var fileMappingItem = new FileMappingItem
            {
                Files = new FileItems("file1"),
                SourceFolder = "folder1"
            };

            var result = JsonUtility.Serialize(fileMappingItem);
            Assert.Equal("{\"files\":[\"file1\"],\"src\":\"folder1\"}", result);
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestManifestItemCollectionConverterCouldSerializeAndDeserialize()
        {
            var manifest = new Manifest();
            ManifestItem manifestItemA = new ManifestItem
            {
                SourceRelativePath = "a"
            };
            ManifestItem manifestItemB = new ManifestItem
            {
                SourceRelativePath = "b"
            };
            manifest.Files.Add(manifestItemA);
            manifest.Files.Add(manifestItemB);

            Assert.Equal(
                "{\"files\":[{\"source_relative_path\":\"a\",\"output\":{},\"is_incremental\":false},{\"source_relative_path\":\"b\",\"output\":{},\"is_incremental\":false}]}",
                JsonUtility.Serialize(manifest));
            Assert.Equal(
                "{\"files\":[{\"source_relative_path\":\"a\",\"output\":{},\"is_incremental\":false},{\"source_relative_path\":\"b\",\"output\":{},\"is_incremental\":false}]}",
                JsonUtility.Serialize(JsonUtility.FromJsonString<Manifest>(JsonUtility.Serialize(manifest))));
        }

        private static object ConvertJObjectToObject(object raw)
        {
            var jValue = raw as JValue;
            if (jValue != null) { return jValue.Value; }
            var jArray = raw as JArray;
            if (jArray != null)
            {
                return jArray.Select(s => ConvertJObjectToObject(s)).ToArray();
            }
            var jObject = raw as JObject;
            if (jObject != null)
            {
                return jObject.ToObject<Dictionary<string, object>>().ToDictionary(p => p.Key, p => ConvertJObjectToObject(p.Value));
            }
            return raw;
        }
    }
}
