// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class SyntaxDetailViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Content)]
    [JsonProperty(Constants.PropertyName.Content)]
    [JsonPropertyName(Constants.PropertyName.Content)]
    public string Content { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Content)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> Contents { get; set; } = [];

    [YamlMember(Alias = "parameters")]
    [JsonProperty("parameters")]
    [JsonPropertyName("parameters")]
    public List<ApiParameter> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonProperty("typeParameters")]
    [JsonPropertyName("typeParameters")]
    public List<ApiParameter> TypeParameters { get; set; }

    /// <summary>
    /// syntax's returns
    /// multiple return type is allowed in languages like JavaScript, Python
    /// ApiParameter supports multiple types
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Return)]
    [JsonProperty(Constants.PropertyName.Return)]
    [JsonPropertyName(Constants.PropertyName.Return)]
    public ApiParameter Return { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Return)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, ApiParameter> ReturnInDevLangs { get; set; } = [];

    [ExtensibleMember]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Text.Json.Serialization.JsonPropertyName("__metadata__")]
    public Dictionary<string, object> Metadata { get; set; } = [];

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    [System.Text.Json.Serialization.JsonInclude]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public CompositeDictionary ExtensionData
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Content, Contents, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Return, ReturnInDevLangs, JTokenConverter.Convert<ApiParameter>)
            .Add(string.Empty, Metadata)
            .Create();
        }
        private init
        {
            // init or getter is required for deserialize data with System.Text.Json.
        }
    }
}
