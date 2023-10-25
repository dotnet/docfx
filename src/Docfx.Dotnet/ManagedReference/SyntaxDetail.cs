// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.ManagedReference;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Dotnet;

internal class SyntaxDetail
{
    [YamlMember(Alias = "content")]
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public SortedList<SyntaxLanguage, string> Content { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<ApiParameter> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonProperty("typeParameters")]
    [JsonPropertyName("typeParameters")]
    public List<ApiParameter> TypeParameters { get; set; }

    [YamlMember(Alias = "return")]
    [JsonProperty("return")]
    [JsonPropertyName("return")]
    public ApiParameter Return { get; set; }
}
