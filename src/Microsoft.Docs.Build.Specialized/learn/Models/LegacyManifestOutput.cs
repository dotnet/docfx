// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation;

public class LegacyManifestOutput
{
    [JsonProperty(".mta.json", NullValueHandling = NullValueHandling.Ignore)]
    public LegacyManifestOutputItem? MetadataOutput { get; set; }

    [JsonProperty(".json", NullValueHandling = NullValueHandling.Ignore)]
    public LegacyManifestOutputItem? TocOutput { get; set; }
}
