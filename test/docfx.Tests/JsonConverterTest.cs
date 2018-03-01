// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class JsonConverterTest
    {
        [Fact]
        [Trait("Related", "docfx")]
        public void TestJObjectDictionaryToObjectDictionaryConverterSerializeAndDeserialize()
        {
            string jsonString = "{" +
                "\"globalMetadata\":{" +
                    "\"layout\":\"Conceptual\"," +
                    "\"breadcrumb_path\":\"/enterprise-mobility/toc.json\"," +
                    "\"product_feedback_displaytext\":\"IntuneFeedback\"," +
                    "\"product_feedback_url\":\"https://microsoftintune.uservoice.com/\"," +
                    "\"contributors_to_exclude\":" +
                        "[\"herohua\",\"fenxu\"]," +
                    "\"searchScope\":[\"Intune\"]," +
                    "\"_op_documentIdPathDepotMapping\":{" +
                        "\"./\":{" +
                            "\"depot_name\":\"Azure.EndUser\"," +
                            "\"folder_relative_path_in_docset\":\".\"" +
                        "}" +
                    "}" +
                "}," +
                "\"noLangKeyword\":false," +
                "\"keepFileLink\":false," +
                "\"cleanupCacheHistory\":false," +
                "\"disableGitFeatures\":false" +
            "}";

            BuildJsonConfig buildOptions = JsonConvert.DeserializeObject<BuildJsonConfig>(jsonString);

            Assert.Equal(buildOptions.GlobalMetadata.Count(), 7);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                ContractResolver = new SkipEmptyOrNullContractResolver()
            };

            Assert.Equal(jsonString, JsonConvert.SerializeObject(buildOptions, settings));
        }

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
            manifest.Files.Add(manifestItemB);
            manifest.Files.Add(manifestItemA);

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

    internal class SkipEmptyOrNullContractResolver : DefaultContractResolver
    {
        public SkipEmptyOrNullContractResolver(bool shareCache = false) : base() { }

        protected override JsonProperty CreateProperty(MemberInfo member,
            MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            bool isDefaultValueIgnored =
            ((property.DefaultValueHandling ?? DefaultValueHandling.Ignore)
             & DefaultValueHandling.Ignore) != 0;
            if (isDefaultValueIgnored
                && !typeof(string).IsAssignableFrom(property.PropertyType)
                && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                Predicate<object> newShouldSerialize = obj => {
                    var collection = property.ValueProvider.GetValue(obj) as ICollection;
                    return collection == null || collection.Count != 0;
                };
                Predicate<object> oldShouldSerialize = property.ShouldSerialize;
                property.ShouldSerialize = oldShouldSerialize != null
                    ? o => oldShouldSerialize(o) && newShouldSerialize(o)
                    : newShouldSerialize;
            }
            return property;
        }
    }
}
