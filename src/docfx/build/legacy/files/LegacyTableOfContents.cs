// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class LegacyTableOfContents
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestOutput legacyManifestOutput)
        {
            var (_, toc) = JsonUtility.Deserialize<LegacyTableOfContentsModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath)));
            ConvertLegacyHref(toc.Items.Select(l => (TableOfContentsItem)l));

            var firstItem = toc?.Items?.FirstOrDefault();
            if (firstItem != null)
            {
                firstItem.PdfAbsolutePath = PathUtility.NormalizeFile(
                    $"/{docset.Config.SiteBasePath}/opbuildpdf/{Path.ChangeExtension(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath, ".pdf")}");

                var dirName = Path.GetDirectoryName(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath);
                firstItem.PdfName = PathUtility.NormalizeFile(
                    $"{(string.IsNullOrEmpty(dirName) ? "" : "/")}{dirName}.pdf");
            }

            File.Delete(docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath));
            context.WriteJson(toc, legacyManifestOutput.TocOutput.ToLegacyOutputPath(docset));
            context.WriteJson(new { }, legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset));
        }

        private static void ConvertLegacyHref(IEnumerable<TableOfContentsItem> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (string.Equals(item.Href, "."))
                {
                    item.Href = "./";
                }

                ConvertLegacyHref(item.Children);
            }
        }

        private class LegacyTableOfContentsItem : TableOfContentsItem
        {
            [JsonProperty(PropertyName = "pdf_absolute_path")]
            public string PdfAbsolutePath { get; set; }

            [JsonProperty(PropertyName = "pdf_name")]
            public string PdfName { get; set; }
        }

        private class LegacyTableOfContentsModel
        {
            [JsonProperty(PropertyName = "items")]
            public List<LegacyTableOfContentsItem> Items { get; set; }
        }
    }
}
