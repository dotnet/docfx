// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiNames
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = "definition")]
    [JsonProperty("definition")]
    public string Definition { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonProperty(Constants.PropertyName.Name)]
    public List<ApiLanguageValuePair<string>> Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonProperty(Constants.PropertyName.NameWithType)]
    public List<ApiLanguageValuePair<string>> NameWithType { get; set; }

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonProperty(Constants.PropertyName.FullName)]
    public List<ApiLanguageValuePair<string>> FullName { get; set; }

    [YamlMember(Alias = "specName")]
    [JsonProperty("specName")]
    public List<ApiLanguageValuePair<string>> Spec { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
