// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal class SyntaxDetail
{
    [YamlMember(Alias = "content")]
    [JsonProperty("content")]
    public SortedList<SyntaxLanguage, string> Content { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    public List<ApiParameter> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonProperty("typeParameters")]
    public List<ApiParameter> TypeParameters { get; set; }

    [YamlMember(Alias = "return")]
    [JsonProperty("return")]
    public ApiParameter Return { get; set; }
}
