// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
