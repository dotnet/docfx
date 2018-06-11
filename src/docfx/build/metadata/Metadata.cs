// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Metadata
    {
        public static JObject GetFromConfig(Document file)
        {
            Debug.Assert(file != null);

            var config = file.Docset.Config;
            var fileMetadata =
                from item in config.FileMetadata
                where item.Match(file.FilePath)
                select item.Value;

            return JsonUtility.Merge(config.GlobalMetadata, fileMetadata);
        }

        public static JObject GenerateRawMetadata(Document file, HtmlNode html, string locale, long wordCount)
        {
            var rawMetadata = new JObject();

            rawMetadata["_op_canonicalUrlPrefix"] = $"https://{file.Docset.Config.HostName}/{file.Docset.Config.Locale}/{file.Docset.Config.SiteBasePath}/";
            rawMetadata["_op_pdfUrlPrefixTemplate"] = $"https://{file.Docset.Config.HostName}/pdfstore/{locale}/{file.Docset.Config.Name}/{{branchName}}{{pdfName}}";

            rawMetadata["_op_wordCount"] = wordCount;

            rawMetadata["depot_name"] = file.Docset.Config.Name;
            rawMetadata["is_dynamic_rendering"] = true;
            rawMetadata["layout"] = file.Docset.Config.GlobalMetadata.TryGetValue("layout", out JToken layout) ? (string)layout : "Conceptual";

            rawMetadata["site_name"] = "Docs";
            rawMetadata["version"] = 0;

            return rawMetadata;
        }
    }
}
