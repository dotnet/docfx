// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.Common;

public class TocItemViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Name)]
    [JsonPropertyName(Constants.PropertyName.Name)]
    public string Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.DisplayName)]
    [JsonPropertyName(Constants.PropertyName.DisplayName)]
    public string DisplayName { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }

    [YamlMember(Alias = "originalHref")]
    [JsonPropertyName("originalHref")]
    public string OriginalHref { get; set; }

    [YamlMember(Alias = Constants.PropertyName.TocHref)]
    [JsonPropertyName(Constants.PropertyName.TocHref)]
    public string TocHref { get; set; }

    [YamlMember(Alias = "originalTocHref")]
    [JsonPropertyName("originalTocHref")]
    public string OriginalTocHref { get; set; }

    [YamlMember(Alias = Constants.PropertyName.TopicHref)]
    [JsonPropertyName(Constants.PropertyName.TopicHref)]
    public string TopicHref { get; set; }

    [YamlMember(Alias = "originalTopicHref")]
    [JsonPropertyName("originalTopicHref")]
    public string OriginalTopicHref { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    public string AggregatedHref { get; set; }

    [YamlMember(Alias = "includedFrom")]
    [JsonPropertyName("includedFrom")]
    public string IncludedFrom { get; set; }

    [YamlMember(Alias = "homepage")]
    [JsonPropertyName("homepage")]
    public string Homepage { get; set; }

    [YamlMember(Alias = "originalHomepage")]
    [JsonPropertyName("originalHomepage")]
    public string OriginalHomepage { get; set; }

    [YamlMember(Alias = "homepageUid")]
    [JsonPropertyName("homepageUid")]
    public string HomepageUid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.TopicUid)]
    [JsonPropertyName(Constants.PropertyName.TopicUid)]
    public string TopicUid { get; set; }

    [YamlMember(Alias = "order")]
    [JsonPropertyName("order")]
    public int? Order { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    public string AggregatedUid { get; set; }

    [YamlMember(Alias = "items")]
    [JsonPropertyName("items")]
    public List<TocItemViewModel> Items { get; set; }

    [YamlIgnore]
    [JsonIgnore]
    public bool IsHrefUpdated { get; set; }

    [ExtensibleMember]
    [JsonIgnore]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [YamlIgnore]
    [JsonExtensionData]
    public CompositeDictionary MetadataJson => CompositeDictionary.CreateBuilder().Add(string.Empty, Metadata).Create();

    public TocItemViewModel Clone()
    {
        var cloned = (TocItemViewModel)MemberwiseClone();
        cloned.Items = Items?.ConvertAll(s => s.Clone());
        return cloned;
    }

    public override string ToString()
    {
        var result = string.Empty;
        result += PropertyInfo(nameof(Name), Name);
        result += PropertyInfo(nameof(Href), Href);
        result += PropertyInfo(nameof(TopicHref), TopicHref);
        result += PropertyInfo(nameof(TocHref), TocHref);
        result += PropertyInfo(nameof(Uid), Uid);
        result += PropertyInfo(nameof(TopicUid), TopicUid);
        return result;

        static string PropertyInfo(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return $"{name}:{value} ";
        }
    }
}
