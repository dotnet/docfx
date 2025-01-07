// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

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

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ContentForCSharp
    {
        get
        {
            Contents.TryGetValue("csharp", out var result);
            return result;
        }
        set
        {
            if (value == null)
            {
                Contents.Remove("csharp");
            }
            else
            {
                Contents["csharp"] = value;
            }
        }
    }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ContentForVB
    {
        get
        {
            Contents.TryGetValue("vb", out var result);
            return result;
        }
        set
        {
            if (value == null)
            {
                Contents.Remove("vb");
            }
            else
            {
                Contents["vb"] = value;
            }
        }
    }

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

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    [System.Text.Json.Serialization.JsonInclude]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public IDictionary<string, object> ExtensionData
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Content, Contents, JTokenConverter.Convert<string>)
            .Create();
        }
        private init
        {
            // init or getter is required for deserialize data with System.Text.Json.
        }
    }
}
