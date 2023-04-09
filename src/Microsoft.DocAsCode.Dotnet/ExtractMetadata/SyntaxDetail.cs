// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
