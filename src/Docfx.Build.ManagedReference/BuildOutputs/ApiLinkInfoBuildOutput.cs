// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.ManagedReference;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiLinkInfoBuildOutput
{
    [YamlMember(Alias = "linkType")]
    [JsonProperty("linkType")]
    [JsonPropertyName("linkType")]
    public LinkType LinkType { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "url")]
    [JsonProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; }

    private bool _needExpand = true;

    public static ApiLinkInfoBuildOutput FromModel(LinkInfo model)
    {
        if (model == null)
        {
            return null;
        }
        if (model.LinkType == LinkType.CRef)
        {
            return new ApiLinkInfoBuildOutput
            {
                LinkType = LinkType.CRef,
                Type = ApiNames.FromUid(model.LinkId),
            };
        }
        else
        {
            return new ApiLinkInfoBuildOutput
            {
                LinkType = LinkType.HRef,
                Url = ApiBuildOutputUtility.GetHref(model.LinkId, model.AltText),
            };
        }
    }

    public static ApiLinkInfoBuildOutput FromModel(LinkInfo model, Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (model == null)
        {
            return null;
        }
        if (model.LinkType == LinkType.CRef)
        {
            return new ApiLinkInfoBuildOutput
            {
                LinkType = LinkType.CRef,
                Type = ApiBuildOutputUtility.GetApiNames(model.LinkId, references, supportedLanguages),
                _needExpand = false,
            };
        }
        else
        {
            return new ApiLinkInfoBuildOutput
            {
                LinkType = LinkType.HRef,
                Url = ApiBuildOutputUtility.GetHref(model.LinkId, model.AltText),
            };
        }
    }

    public void Expand(Dictionary<string, ApiReferenceBuildOutput> references, string[] supportedLanguages)
    {
        if (_needExpand)
        {
            _needExpand = false;
            Type = ApiBuildOutputUtility.GetApiNames(Type?.Uid, references, supportedLanguages);
        }
    }
}
