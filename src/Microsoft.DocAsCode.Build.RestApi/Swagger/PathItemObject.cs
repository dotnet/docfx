// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.RestApi.Swagger;

/// <summary>
/// TODO: need a converter
/// </summary>
[Serializable]
public class PathItemObject
{
    /// <summary>
    /// A list of parameters that are applicable for all the operations described under this path.
    /// These parameters can be overridden at the operation level, but cannot be removed there.
    /// </summary>
    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    public List<ParameterObject> Parameters { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
