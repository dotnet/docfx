// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class MetadataValidator
    {
        private static readonly HashSet<string> s_reservedNames = GetReservedMetadata();
        private static readonly ConcurrentDictionary<string, Lazy<Type>> s_fileMetadataTypes = new ConcurrentDictionary<string, Lazy<Type>>(StringComparer.OrdinalIgnoreCase);

        public static List<Error> Validate(JObject metadata)
        {
            var errors = new List<Error>();
            foreach (var (key, token) in metadata)
            {
                if (s_reservedNames.Contains(key))
                {
                    errors.Add(Errors.ReservedMetadata(JsonUtility.GetSourceInfo(token), key, token.Path));
                }
            }
            return errors;
        }

        private static HashSet<string> GetReservedMetadata()
        {
            var legacyBlackList = new[]
            {
                "content_type", "document_id", "ms.documentid", "internal_document_id", "locale", "ms.contentlang", "ms.locale", "product_family", "product_version",
                "search.ms_sitename", "search.ms_product", "search.ms_docsetname", "updated_at", "ms.publishtime", "toc_asset_id", "original_content_git_url", "ms.giturl",
                "original_ref_skeleton_git_url", "ms.gitsourceurl", "toc_rel", "site_name", "ms.sitename", "area", "theme", "theme_branch", "theme_url", "is_active",
                "gitcommit", "ms.gitcommit", "ref_skeleton_gitcommit", "ms.gitsourcecommit", "Product", "TopicType", "APIType", "APILocation", "APIName", "APIExtraInfo",
                "TargetOS", "sitemap_priority", "AmbientContext", "MN", "ms.auth", "ms.lang", "ms.loc", "ms.prodver", "ms.puidhash", "ms.contentsource", "depot_name",
                "ms.depotname", "pagetype", "ms.opspagetype", "word_count", "content_uri", "publish_version", "canonical_url", "relative_path_to_theme_resources",
                "is_dynamic_rendering", "need_preview_pull_request", "moniker_type", "is_significant_update", "document_version_independent_id", "serviceData", "is_hidden",
            };

            var blackList = new HashSet<string>(JsonUtility.GetPropertyNames(typeof(OutputModel)).Concat(legacyBlackList));

            foreach (var name in JsonUtility.GetPropertyNames(typeof(InputMetadata)))
            {
                blackList.Remove(name);
            }

            return blackList;
        }
    }
}
