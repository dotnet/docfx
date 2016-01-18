// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Utility;

    public class MetadataModel
    {
        public MetadataItem TocYamlViewModel { get; set; }
        public List<MetadataItem> Members { get; set; }
    }

    public class MetadataModelUtility
    {
        public static string ResolveApiHrefRelativeToCurrentApi(ApiReferenceModel index, string name, string currentApiName)
        {
            if (string.IsNullOrEmpty(name) || index == null) return name;
            ApiIndexItemModel item;
            if (index.TryGetValue(name, out item))
            {
                ApiIndexItemModel currentApi;
                if (!index.TryGetValue(currentApiName, out currentApi)) return item.Href;
                var currentHref = currentApi.Href;
                if (string.IsNullOrEmpty(currentHref)) return item.Href;
                var directoryName = Path.GetDirectoryName(currentHref);
                return PathUtility.MakeRelativePath(directoryName, item.Href);
            }
            
            // If unable to resolve the Api, return null as href
            return null;
        }
    }
}
