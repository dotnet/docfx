// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyCrrInfo
    {
        public static void Convert(Docset docset, Context context)
        {
            var legacyCrrInfoItems = new List<LegacyCrrInfoItem>();

            foreach (var dependentRepo in docset.Config.Dependencies)
            {
                var (_, url, branch) = Restore.GetGitRestoreInfo(dependentRepo.Value);
                legacyCrrInfoItems.Add(new LegacyCrrInfoItem
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

            context.WriteJson(crrInfo, Path.Combine(docset.Config.SiteBasePath, "op_crr_info.json"));
        }
    }
}
