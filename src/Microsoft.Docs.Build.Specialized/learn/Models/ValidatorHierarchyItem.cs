// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public class ValidatorHierarchyItem : IValidateModel
{
    [JsonProperty("serviceData")]
    public string ServiceData { get; set; } = "";

    [JsonProperty("source_relative_path")]
    public string SourceRelativePath { get; set; } = "";

    public bool IsValid { get; set; }

    public bool IsDeleted { get; set; }

    [JsonProperty("uid")]
    public string UId { get; set; } = "";

    public string Uid => UId;

    public IValidateModel? Parent { get; set; }

    [JsonProperty("ms.date")]
    public string? MSDate { get; set; }

    [JsonProperty("updated_at")]
    public string PublishUpdatedAt { get; set; } = "";

    [JsonProperty("page_kind")]
    public string PageKind { get; set; } = "";

    [JsonProperty("depotName")]
    public string DepotName { get; set; } = "";

    [JsonProperty("assetId")]
    public string? AssetId { get; set; }

    [JsonProperty("locale")]
    public string Locale { get; set; } = "";

    [JsonProperty("branch")]
    public string Branch { get; set; } = "";

    [JsonProperty("url")]
    public string Url { get; set; } = "";

    [JsonProperty("abstract")]
    public string Abstract { get; set; } = "";

    [JsonProperty("summary")]
    public string Summary { get; set; } = "";

    [JsonProperty("points", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int Points { get; set; } = 0;
}
