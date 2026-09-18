// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class SourceLinkFilterTest
{
    [Theory]
    [InlineData("**/obj/**", "obj/Generated.g.cs", true)]
    [InlineData("**/obj/**", "/repo/obj/Generated.g.cs", true)]
    [InlineData("**/obj/**", @"C:\repo\obj\Generated.g.cs", true)]
    [InlineData("**/obj/**", @"\\server\repo\obj\Generated.g.cs", true)]
    [InlineData("**/obj/**", @"C:\repo/obj\Generated.g.cs", true)]
    [InlineData("**/obj/**", "/home/user/.cache/repo/obj/Generated.g.cs", true)]
    [InlineData("**/*.cs", "/repo/.generated/Generated.cs", true)]
    [InlineData("**/obj/**", "/repo/Obj/Generated.g.cs", true)]
    [InlineData("**/obj/**", "/repo/objects/Generated.g.cs", false)]
    [InlineData("**/obj/**", "/repo/obj.cs", false)]
    [InlineData("**/MethodsForPropertiesGenerator/**", "NetCord/obj/MethodsForPropertiesGenerator/Generator/Generated.g.cs", true)]
    [InlineData("**/MethodsForPropertiesGenerator/**", "NetCord/obj/OtherGenerator/Generated.g.cs", false)]
    [InlineData("**/MethodsForPropertiesGenerator/**", "/NetCord/Rest/MessageProperties.cs", false)]
    [InlineData("**/MethodsForPropertiesGenerator/**", @"NetCord\obj\MethodsForPropertiesGenerator\Generator\NetCord.Rest.MessageProperties.g.cs", true)]
    [InlineData("**/MethodsForPropertiesGenerator/**", "/NetCord/Generated/CheckedIn.g.cs", false)]
    [InlineData("**/NetCord.Rest.MessageProperties.g.cs", "NetCord/obj/MethodsForPropertiesGenerator/Generator/NetCord.Rest.MessageProperties.g.cs", true)]
    [InlineData("**/NetCord.Rest.MessageProperties.g.cs", @"C:\repo\NetCord\obj\MethodsForPropertiesGenerator\Generator\NetCord.Rest.MessageProperties.g.cs", true)]
    [InlineData("**/NetCord.Rest.MessageProperties.g.cs", "NetCord/obj/MethodsForPropertiesGenerator/Generator/NetCord.Rest.UserProperties.g.cs", false)]
    [InlineData("**/*.g.cs", "/repo/generated/Generated.g.cs", true)]
    [InlineData("**/.generated/**", "/repo/.generated/Generated.cs", true)]
    [InlineData("**/{Generated,Temporary}/**", "/repo/Temporary/Generated.cs", true)]
    public void MatchesDocumentPaths(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, new SourceLinkFilter([pattern]).IsExcluded(path));
    }

    [Fact]
    public void EmptyConfigurationDoesNotExcludeAnySource()
    {
        Assert.False(new SourceLinkFilter(null).IsExcluded("/repo/obj/Generated.g.cs"));
        Assert.False(new SourceLinkFilter([]).IsExcluded("/repo/obj/Generated.g.cs"));
        Assert.True(new SourceLinkFilter(["**/other/**", "**/obj/**"]).IsExcluded("/repo/obj/Generated.g.cs"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyPatterns(string pattern)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new SourceLinkFilter([pattern]));
        Assert.Equal("sourceLinkExclude", exception.ParamName);
    }

    [Fact]
    public void BothJsonSerializersReadSourceLinkExclude()
    {
        const string json = """{"sourceLinkExclude":["**/Generated/**"]}""";
        Assert.Equal(["**/Generated/**"], JsonSerializer.Deserialize<MetadataJsonItemConfig>(json).SourceLinkExclude);
        Assert.Equal(["**/Generated/**"], Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataJsonItemConfig>(json).SourceLinkExclude);
        Assert.Empty(JsonSerializer.Deserialize<MetadataJsonItemConfig>("{}").SourceLinkExclude);
        Assert.Empty(Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataJsonItemConfig>("{}").SourceLinkExclude);
        Assert.False(JsonSerializer.Deserialize<MetadataJsonItemConfig>("{}").ExcludeGeneratedSourceLinks);
        Assert.False(Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataJsonItemConfig>("{}").ExcludeGeneratedSourceLinks);
        const string automatic = """{"excludeGeneratedSourceLinks":true}""";
        Assert.True(JsonSerializer.Deserialize<MetadataJsonItemConfig>(automatic).ExcludeGeneratedSourceLinks);
        Assert.True(Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataJsonItemConfig>(automatic).ExcludeGeneratedSourceLinks);
    }
}
