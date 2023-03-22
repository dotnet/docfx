// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.UniversalReference;

[Serializable]
public class ApiLanguageValuePairWithLevel<T> : ApiLanguageValuePair<T>
{
    [YamlMember(Alias = "level")]
    [JsonProperty("level")]
    public int Level { get; set; }
}