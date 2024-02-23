// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.Common;

public class ReferenceViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "parent")]
    [JsonPropertyName("parent")]
    public string Parent { get; set; }

    [YamlMember(Alias = "definition")]
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [YamlMember(Alias = "isExternal")]
    [JsonPropertyName("isExternal")]
    public bool? IsExternal { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Name)]
    [JsonIgnore]
    public SortedList<string, string> NameInDevLangs { get; private set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.NameWithType)]
    [JsonPropertyName(Constants.PropertyName.NameWithType)]
    public string NameWithType { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.NameWithType)]
    [JsonIgnore]
    public SortedList<string, string> NameWithTypeInDevLangs { get; private set; } = new SortedList<string, string>();

    [YamlMember(Alias = Constants.PropertyName.FullName)]
    [JsonPropertyName(Constants.PropertyName.FullName)]
    public string FullName { get; set; }

    [ExtensibleMember(Constants.ExtensionMemberPrefix.FullName)]
    [JsonIgnore]
    public SortedList<string, string> FullNameInDevLangs { get; private set; } = new SortedList<string, string>();

    [ExtensibleMember(Constants.ExtensionMemberPrefix.Spec)]
    [JsonIgnore]
    public SortedList<string, List<SpecViewModel>> Specs { get; private set; } = new SortedList<string, List<SpecViewModel>>();

    [ExtensibleMember]
    [JsonIgnore]
    public Dictionary<string, object> Additional { get; private set; } = new Dictionary<string, object>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [JsonExtensionData]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public CompositeDictionary AdditionalJson =>
        CompositeDictionary
            .CreateBuilder()
            .Add(Constants.ExtensionMemberPrefix.Name, NameInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.NameWithType, NameWithTypeInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.FullName, FullNameInDevLangs, JTokenConverter.Convert<string>)
            .Add(Constants.ExtensionMemberPrefix.Spec, Specs, JTokenConverter.Convert<List<SpecViewModel>>)
            .Add(string.Empty, Additional)
            .Create();

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
