// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class LegacyFileMapItem
{
    public LegacyItemType Type { get; set; }

    public string OutputRelativePath { get; set; }

    public string AssetId { get; set; }

    public string? Version { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool IsMonikerRange { get; set; } = true;

    public MonikerList Monikers { get; set; }

    public bool ShouldSerializeIsMonikerRange() => !string.IsNullOrEmpty(Version);

    public LegacyFileMapItem(
        string legacyOutputFilePathRelativeToBasePath,
        string legacySiteUrlRelativeToBasePath,
        ContentType contentType,
        string? version,
        MonikerList monikers)
    {
        switch (contentType)
        {
            case ContentType.Page:
            case ContentType.Redirection:
                Type = LegacyItemType.Content;
                OutputRelativePath = PathUtility.NormalizeFile(
                    LegacyUtility.ChangeExtension(legacyOutputFilePathRelativeToBasePath, ".html"));
                AssetId = legacySiteUrlRelativeToBasePath;
                Version = version;
                Monikers = monikers;
                break;
            case ContentType.Resource:
                Type = LegacyItemType.Resource;
                OutputRelativePath = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
                AssetId = legacySiteUrlRelativeToBasePath;
                Version = version;
                Monikers = monikers;
                break;
            case ContentType.Toc:
                Type = LegacyItemType.Toc;
                OutputRelativePath = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
                AssetId = legacySiteUrlRelativeToBasePath;
                Version = version;
                Monikers = monikers;
                break;
            default:
                throw new NotSupportedException($"{contentType} is not supported");
        }
    }

    public static LegacyFileMapItem Instance(
        string legacyOutputFilePathRelativeToBasePath,
        string legacySiteUrlRelativeToBasePath,
        ContentType contentType,
        string? version,
        MonikerList monikers)
    {
        return new LegacyFileMapItem(
            legacyOutputFilePathRelativeToBasePath, legacySiteUrlRelativeToBasePath, contentType, version, monikers);
    }
}
