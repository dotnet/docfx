// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    internal static class Helper
    {
        public static void AddFileLinkSource(this Dictionary<string, List<LinkSourceInfo>> fileLinkSources, LinkSourceInfo source)
        {
            var file = source.Target;
            if (!fileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
            {
                sources = new List<LinkSourceInfo>();
                fileLinkSources[file] = sources;
            }
            sources.Add(source);
        }
    }
}
