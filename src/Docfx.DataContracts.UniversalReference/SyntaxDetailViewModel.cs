// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class SyntaxDetailViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Content)]
    [JsonPropertyName(Constants.PropertyName.Content)]
    public string Content { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Content)]
    [JsonIgnore]
    public SortedList<string, string> Contents { get; set; } = new SortedList<string, string>();

    [YamlMember(Alias = "parameters")]
    [JsonPropertyName("parameters")]
    public List<ApiParameter> Parameters { get; set; }

    [YamlMember(Alias = "typeParameters")]
    [JsonPropertyName("typeParameters")]
    public List<ApiParameter> TypeParameters { get; set; }

    /// <summary>
    /// syntax's returns
    /// multiple return type is allowed in languages like JavaScript, Python
    /// ApiParameter supports multiple types
    /// </summary>
    [YamlMember(Alias = Constants.PropertyName.Return)]
    [JsonPropertyName(Constants.PropertyName.Return)]
    public ApiParameter Return { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Return)]
    [JsonIgnore]
    public SortedList<string, ApiParameter> ReturnInDevLangs { get; set; } = new SortedList<string, ApiParameter>();

    [ExtensibleMember]
    [JsonIgnore]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [JsonExtensionData]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public CompositeDictionary ExtensionData =>
        CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Content, Contents, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Return, ReturnInDevLangs, JTokenConverter.Convert<ApiParameter>)
            .Add(string.Empty, Metadata)
            .Create();
}
