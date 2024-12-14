// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Plugins;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.Engine;

/// <summary>
/// Xrefmap.
/// </summary>
public class XRefMap : IXRefContainer
{
    /// <summary>
    /// Indicate <see cref="References"/> are sorted by <see cref="XRefSpec.Uid"/> with or not.
    /// </summary>
    [YamlMember(Alias = "sorted")]
    [JsonProperty("sorted")]
    [JsonPropertyName("sorted")]
    public bool? Sorted { get; set; }

    /// <summary>
    /// Indicate href links are updated or not.
    /// </summary>
    [YamlMember(Alias = "hrefUpdated")]
    [JsonProperty("hrefUpdated")]
    [JsonPropertyName("hrefUpdated")]
    public bool? HrefUpdated { get; set; }

    /// <summary>
    /// Base url. It's used when href is specified as relative url.
    /// </summary>
    [YamlMember(Alias = "baseUrl")]
    [JsonProperty("baseUrl")]
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; }

    /// <summary>
    /// List of <see href="XRefMapRedirection"/> settings.
    /// </summary>
    [YamlMember(Alias = "redirections")]
    [JsonProperty("redirections")]
    [JsonPropertyName("redirections")]
    public List<XRefMapRedirection> Redirections { get; set; }

    /// <summary>
    /// List of <see href="XRefSpec"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="Sorted"/> is true. XRefSpec items must be sorted by <see cref="XRefSpec.Uid"/> with InvariantCulture.
    /// </remarks>
    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
    [JsonPropertyName("references")]
    public List<XRefSpec> References { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Others { get; set; } = [];

    public void Sort()
    {
        if (Sorted == true)
        {
            return;
        }
        References?.Sort(XRefSpecUidComparer.Instance);
        Sorted = true;
    }

    public void UpdateHref(Uri baseUri)
    {
        if (HrefUpdated == true)
        {
            return;
        }
        if (References == null)
        {
            return;
        }
        var list = new List<XRefSpec>(References.Count);
        foreach (var r in References)
        {
            if (!Uri.TryCreate(r.Href, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                Logger.LogWarning($"Bad uri in xref map: {r.Href}");
                continue;
            }
            if (uri.IsAbsoluteUri)
            {
                list.Add(r);
            }
            else
            {
                list.Add(new XRefSpec(r) { Href = new Uri(baseUri, uri).AbsoluteUri });
            }
        }
        References = list;
        HrefUpdated = true;
    }

    [YamlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmbeddedRedirections => false;

    public IEnumerable<XRefMapRedirection> GetRedirections() =>
        Redirections ?? Enumerable.Empty<XRefMapRedirection>();

    public IXRefContainerReader GetReader()
    {
        return new BasicXRefMapReader(this);
    }
}
