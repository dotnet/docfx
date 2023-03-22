// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.Common;

[Serializable]
public class SpecViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    public string Name { get; set; }

    [YamlMember(Alias = "isExternal")]
    [JsonProperty("isExternal")]
    public bool IsExternal { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonProperty(Constants.PropertyName.Href)]
    public string Href { get; set; }
}
