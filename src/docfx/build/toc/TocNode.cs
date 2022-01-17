// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class TocNode
{
    public SourceInfo<string?> Name { get; set; }

    public string? DisplayName { get; set; }

    public SourceInfo<string?> Href { get; set; }

    public SourceInfo<string?> TopicHref { get; set; }

    public SourceInfo<string?> TocHref { get; set; }

    public string? Homepage { get; set; }

    public SourceInfo<string?> Uid { get; set; }

    public SourceInfo<LandingPageType?> LandingPageType { get; set; }

    public static bool ShouldSerializeLandingPageType() => false;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool Expanded { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool MaintainContext { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public MonikerList Monikers { get; set; }

    public List<SourceInfo<TocNode>> Items { get; set; } = new List<SourceInfo<TocNode>>();

    public string[] Children { get; set; } = Array.Empty<string>();

    public static bool ShouldSerializeChildren() => false;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    [JsonIgnore]
    public FilePath? Document { get; set; }

    public TocNode() { }

    public TocNode(TocNode item)
    {
        Name = item.Name;
        DisplayName = item.DisplayName;
        Href = item.Href;
        TopicHref = item.TopicHref;
        TocHref = item.TocHref;
        Homepage = item.Homepage;
        Uid = item.Uid;
        Expanded = item.Expanded;
        MaintainContext = item.MaintainContext;
        ExtensionData = item.ExtensionData;
        Document = item.Document;
        Children = item.Children;
        LandingPageType = item.LandingPageType;
    }
}
