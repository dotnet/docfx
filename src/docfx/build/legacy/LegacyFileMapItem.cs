// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyFileMapItem
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "output_relative_path")]
        public string OutputRelativePath { get; set; }

        [JsonProperty(PropertyName = "asset_id")]
        public string AssetId { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string? Version { get; set; }

        [JsonProperty(PropertyName = "is_moniker_range")]
        public bool IsMonikerRange { get; set; } = true;

        [JsonProperty(PropertyName = "monikers")]
        public IReadOnlyList<string> Monikers { get; set; } = Array.Empty<string>();

        public bool ShouldSerializeIsMonikerRange() => !string.IsNullOrEmpty(Version);

        public LegacyFileMapItem(
            string legacyOutputFilePathRelativeToBasePath,
            string legacySiteUrlRelativeToBasePath,
            ContentType contentType,
            string? version,
            IReadOnlyList<string> monikers)
        {
            switch (contentType)
            {
                case ContentType.Page:
                case ContentType.Redirection:
                    Type = "Content";
                    OutputRelativePath = PathUtility.NormalizeFile(
                        LegacyUtility.ChangeExtension(legacyOutputFilePathRelativeToBasePath, ".html"));
                    AssetId = legacySiteUrlRelativeToBasePath;
                    Version = version;
                    Monikers = monikers;
                    break;
                case ContentType.Resource:
                    Type = "Resource";
                    OutputRelativePath = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
                    AssetId = legacySiteUrlRelativeToBasePath;
                    Version = version;
                    Monikers = monikers;
                    break;
                case ContentType.TableOfContents:
                default:
                    throw new NotSupportedException($"{contentType} is not supported");
            }
        }

        public static LegacyFileMapItem? Instance(
            string legacyOutputFilePathRelativeToBasePath,
            string legacySiteUrlRelativeToBasePath,
            ContentType contentType,
            string? version,
            IReadOnlyList<string> monikers)
        {
            if (contentType == ContentType.TableOfContents || contentType == ContentType.Unknown)
            {
                return null;
            }

            return new LegacyFileMapItem(
                legacyOutputFilePathRelativeToBasePath, legacySiteUrlRelativeToBasePath, contentType, version, monikers);
        }

        private static string RemoveExtension(string path)
        {
            return path.Substring(0, path.LastIndexOf('.'));
        }
    }
}
