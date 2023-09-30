// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.RestApi.Swagger;

/// <summary>
/// Info object
/// </summary>
public class InfoObject
{
    /// <summary>
    /// Required. The title of the application.
    /// </summary>
    [YamlMember(Alias = "title")]
    [JsonProperty("title")]
    public string Title { get; set; }

    /// <summary>
    /// Required. Provides the version of the application API
    /// </summary>
    [YamlMember(Alias = "version")]
    [JsonProperty("version")]
    public string Version { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> PatternedObjects { get; set; } = new Dictionary<string, object>();
}
