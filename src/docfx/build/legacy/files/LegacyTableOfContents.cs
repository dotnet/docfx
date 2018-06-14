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
            var toc = JsonUtility.Deserialize<LegacyTableOfContentsModel>(File.ReadAllText(Path.Combine(docset.Config.Output.Path, doc.OutputPath)));
            var tocItemWithPath = toc?.Items?.FirstOrDefault();
            if (tocItemWithPath != null)
            {
                tocItemWithPath.PdfAbsolutePath = PathUtility.NormalizeFile(
                    $"/{docset.Config.SiteBasePath}/opbuildpdf/{Path.ChangeExtension(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath, ".pdf")}");
                tocItemWithPath.PdfName = PathUtility.NormalizeFile(
                    $"/{Path.GetDirectoryName(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath)}.pdf");
            }

            context.Delete(doc.OutputPath);
            context.WriteJson(toc, legacyManifestOutput.TocOutput.ToLegacyOutputPath(docset));
            context.WriteJson(new { }, legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset));
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
