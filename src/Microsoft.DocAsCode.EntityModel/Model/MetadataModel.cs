namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.DocAsCode.Utility;

    public class MetadataModel
    {
        public ApiReferenceModel Indexer { get; set; }
        public MetadataItem TocYamlViewModel { get; set; }
        public List<MetadataItem> Members { get; set; }
    }

    public class MetadataModelUtility
    {
        public static string ResolveApiHrefRelativeToCurrent(Dictionary<string, MetadataItem> index, string name, string currentHref)
        {
            if (string.IsNullOrEmpty(name) || index == null) return name;
            MetadataItem item;
            if (index.TryGetValue(name, out item))
            {
                if (string.IsNullOrEmpty(currentHref)) return item.Href;
                var directoryName = Path.GetDirectoryName(currentHref);
                return FileExtensions.MakeRelativePath(directoryName, item.Href);
            }

            return name;
        }

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
                return FileExtensions.MakeRelativePath(directoryName, item.Href);
            }
            
            // If unable to resolve the Api, return null as href
            return null;
        }
    }
}
