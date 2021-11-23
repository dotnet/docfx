// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class LegacyManifestItem
{
    public string AssetId { get; set; } = "";

    public string SourceRelativePath { get; set; } = "";

    public string OriginalType { get; set; } = "";

    public LegacyManifestOutput? Output { get; set; }
}
