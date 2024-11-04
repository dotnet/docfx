// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using Docfx.Common;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Docfx.Tests;

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
            "\"disableGitFeatures\":false" +
        "}";

        BuildJsonConfig buildOptions = JsonConvert.DeserializeObject<BuildJsonConfig>(jsonString);

        Assert.Equal(7, buildOptions.GlobalMetadata.Count);

        JsonSerializerSettings settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            ContractResolver = new SkipEmptyOrNullContractResolver()
        };

        Assert.Equal(jsonString, JsonConvert.SerializeObject(buildOptions, settings), ignoreLineEndingDifferences: true);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public void TestFileMetadataPairsConverterCouldSerializeAndDeserialize()
    {
        FileMetadataPairs item = new(
            new List<FileMetadataPairsItem>
            {
                new("*.md", 1L),
                new("*.m", true),
                new("abc", "string"),
                new("/[]\\*.cs", new Dictionary<string, object>{ ["key"] = "2" }),
                new("*/*.cs", new object[] { "1", "2" }),
                new("**", new Dictionary<string, object>{ ["key"] = new object[] {"1", "2" } }),
            });

        var result = JsonUtility.Serialize(item);
        Assert.Equal("{\"*.md\":1,\"*.m\":true,\"abc\":\"string\",\"/[]\\\\*.cs\":{\"key\":\"2\"},\"*/*.cs\":[\"1\",\"2\"],\"**\":{\"key\":[\"1\",\"2\"]}}", result);
        using var reader = new StringReader(result);
        var pairs = JsonUtility.Deserialize<FileMetadataPairs>(reader);
        Assert.Equal(item.Count, pairs.Count);

        // Assert
        pairs.Should().BeEquivalentTo(item);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public void TestFileMappingItemSrcInputShouldWork()
    {
        var input = "{\"files\":[\"file1\"],\"src\":\"folder1\"}";
        using var sr = new StringReader(input);
        var result = JsonUtility.Deserialize<FileMappingItem>(sr);
        Assert.Equal("folder1", result.Src);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public void TestFileMappingItemOutputShouldContainSrcOnly()
    {
        var fileMappingItem = new FileMappingItem
        {
            Files = new FileItems("file1"),
            Src = "folder1"
        };

        var result = JsonUtility.Serialize(fileMappingItem);
        Assert.Equal("{\"files\":[\"file1\"],\"src\":\"folder1\"}", result);
    }
}

internal class SkipEmptyOrNullContractResolver : DefaultContractResolver
{
    public SkipEmptyOrNullContractResolver(bool shareCache = false)
    { }

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
            bool newShouldSerialize(object obj)
            {
                return property.ValueProvider.GetValue(obj) is not ICollection collection || collection.Count != 0;
            }
            Predicate<object> oldShouldSerialize = property.ShouldSerialize;
            property.ShouldSerialize = oldShouldSerialize != null
                ? o => oldShouldSerialize(o) && newShouldSerialize(o)
                : newShouldSerialize;
        }
        return property;
    }
}
