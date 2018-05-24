// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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

        public LegacyFileMapItem(string legacyOutputFilePathRelativeToSiteBasePath, ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    Type = "Content";
                    OutputRelativePath = PathUtility.NormalizeFile(Path.ChangeExtension(legacyOutputFilePathRelativeToSiteBasePath, ".html"));
                    AssetId = PathUtility.NormalizeFile(RemoveExtension(legacyOutputFilePathRelativeToSiteBasePath));
                    break;
                case ContentType.Asset:
                    Type = "Resource";
                    OutputRelativePath = AssetId = PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
                    break;
                case ContentType.TableOfContents:
                default:
                    throw new NotSupportedException($"{contentType} is not supported");
            }
        }

        public static LegacyFileMapItem Instance(string legacyOutputFilePathRelativeToSiteBasePath, ContentType contentType)
        {
            if (contentType == ContentType.TableOfContents || contentType == ContentType.Unknown)
            {
                return null;
            }

            return new LegacyFileMapItem(legacyOutputFilePathRelativeToSiteBasePath, contentType);
        }

        private static string RemoveExtension(string path)
        {
            return path.Substring(0, path.LastIndexOf('.'));
        }
    }
}
