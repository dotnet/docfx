// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.Common;

public class ReferenceViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonProperty("parent")]
    [JsonPropertyName("parent")]
    public string Parent { get; set; }

    [YamlMember(Alias = "definition")]
    [JsonProperty("definition")]
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [JsonProperty("isExternal")]
    [YamlMember(Alias = "isExternal")]
    [JsonPropertyName("isExternal")]
    public bool? IsExternal { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonProperty(Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonProperty(Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> NameInDevLangs { get; private set; } = [];

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonProperty(Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> NameWithTypeInDevLangs { get; private set; } = [];

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonProperty(Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, string> FullNameInDevLangs { get; private set; } = [];

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Spec)]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public SortedList<string, List<SpecViewModel>> Specs { get; private set; } = [];

    [ExtensibleMember]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, object> Additional { get; private set; } = [];

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    [System.Text.Json.Serialization.JsonInclude]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public CompositeDictionary AdditionalJson
    {
        get
        {
            return CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Name, NameInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.NameWithType, NameWithTypeInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.FullName, FullNameInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Spec, Specs, JTokenConverter.Convert<List<SpecViewModel>>)
            .Add(string.Empty, Additional)
            .Create();
        }
        private init
        {
            // init or getter is required for deserialize data with System.Text.Json.
        }
    }

    public ReferenceViewModel Clone()
    {
        var copied = (ReferenceViewModel)MemberwiseClone();
        copied.Additional = new Dictionary<string, object>(Additional);
        copied.FullNameInDevLangs = new SortedList<string, string>(FullNameInDevLangs);
        copied.NameInDevLangs = new SortedList<string, string>(NameInDevLangs);
        copied.NameWithTypeInDevLangs = new SortedList<string, string>(NameWithTypeInDevLangs);
        copied.Specs = new SortedList<string, List<SpecViewModel>>(Specs.ToDictionary(s => s.Key, s => new List<SpecViewModel>(s.Value)));
        return copied;
    }
}
