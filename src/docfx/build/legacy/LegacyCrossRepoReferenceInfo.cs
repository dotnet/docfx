// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class LegacyCrossRepoReferenceInfo
    {
        public static void Convert(Docset docset, Context context)
        {
            var legacyCrrInfoItems = new List<LegacyCrossRepoReferenceInfoItem>();

            if (!string.IsNullOrEmpty(docset.Config.Theme))
            {
                var (url, branch) = LocalizationUtility.GetLocalizedTheme(docset.Config.Theme, docset.Locale, docset.Config.Localization.DefaultLocale);

                legacyCrrInfoItems.Add(new LegacyCrossRepoReferenceInfoItem
                {
                    PathToRoot = "_themes",
                    Url = url,
                    Branch = branch,
                });
            }

            foreach (var dependentRepo in docset.Config.Dependencies)
            {
                var (url, branch) = HrefUtility.SplitGitHref(dependentRepo.Value);
                legacyCrrInfoItems.Add(new LegacyCrossRepoReferenceInfoItem
                {
                    PathToRoot = dependentRepo.Key,
                    Url = url,
                    Branch = branch,
                });
            }

            object crrInfo = legacyCrrInfoItems;
            if (legacyCrrInfoItems.Count == 1)
            {
                crrInfo = legacyCrrInfoItems[0];
            }

            context.Output.WriteJson(crrInfo, Path.Combine(docset.Config.DocumentId.SiteBasePath, "op_crr_info.json"));
        }

        private class LegacyCrossRepoReferenceInfoItem
        {
            [JsonProperty("path_to_root")]
            public string PathToRoot { get; set; }

            [JsonProperty("branch")]
            public string Branch { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }
    }
}
