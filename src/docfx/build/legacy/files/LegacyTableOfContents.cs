// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            ConvertLegacyItems(toc.Items);

            toc.Metadata = toc.Metadata ?? new LegacyTableOfContentsMetadata();

            var (pdfName, pdfAbsolutePath) = PdfInfo();
            toc.Metadata.PdfName = pdfName;
            toc.Metadata.PdfAbsolutePath = pdfAbsolutePath;

            File.Delete(docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath));
            context.WriteJson(toc, legacyManifestOutput.TocOutput.ToLegacyOutputPath(docset));
            context.WriteJson(
                new LegacyTableOfContentsExperimentMetadata
                {
                    Experimental = toc.Metadata.Experimental,
                    ExperimentId = toc.Metadata.ExperimentId,
                },
                legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset));

            (string, string) PdfInfo()
            {
                var dirName = Path.GetDirectoryName(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath);
                var isExperimentalSource = toc.Metadata.Experimental.HasValue && !toc.Metadata.Experimental.Value;

                // Metadata Experimental cases:
                // true: possible to redirect to corresponding experimental file on requested
                // false: explicitly declared as experimental pdf file
                // default(null): normal TOC with no experiment
                if (isExperimentalSource)
                {
                    Debug.Assert(doc.OutputPath.Contains(".experimental", StringComparison.OrdinalIgnoreCase));
                }

                // keep the v2 output format if it's experimental
                // e.g. "cosmos-db/TOC.experimental.pdf"
                var l_pdfName = isExperimentalSource ?
                    Path.ChangeExtension(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath, ".pdf") :
                    PathUtility.NormalizeFile(
                $"{(string.IsNullOrEmpty(dirName) ? "" : "/")}{dirName}.pdf");
                var l_pdfAbsolutePath = PathUtility.NormalizeFile(
                $"/{docset.Config.SiteBasePath}/opbuildpdf/{Path.ChangeExtension(legacyManifestOutput.TocOutput.OutputPathRelativeToSiteBasePath, ".pdf")}");

                return (l_pdfName, l_pdfAbsolutePath);
            }
        }

        private static void ConvertLegacyItems(IEnumerable<LegacyTableOfContentsItem> items)
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

                if (item.TocHref != null)
                {
                    // that's breadcrumbs
                    Debug.Assert(HrefUtility.IsAbsoluteHref(item.TocHref));
                    item.HomePage = item.Href; // href is got from topic href or href of input model
                    item.Href = item.TocHref; // set href to toc href for backward compatibility
                }

                ConvertLegacyItems(item.Children);
            }
        }

        private sealed class LegacyTableOfContentsMetadata : LegacyTableOfContentsExperimentMetadata
        {
            [JsonProperty(PropertyName = "pdf_absolute_path")]
            public string PdfAbsolutePath { get; set; }

            [JsonProperty(PropertyName = "pdf_name")]
            public string PdfName { get; set; }
        }

        private class LegacyTableOfContentsExperimentMetadata
        {
            [JsonProperty(PropertyName = "experiment_id")]
            public string ExperimentId { get; set; }

            [JsonProperty(PropertyName = "experimental")]
            public bool? Experimental { get; set; }
        }

        private class LegacyTableOfContentsModel
        {
            [JsonProperty(PropertyName = "items")]
            public List<LegacyTableOfContentsItem> Items { get; set; }

            [JsonProperty(PropertyName = "metadata", NullValueHandling = NullValueHandling.Ignore)]
            public LegacyTableOfContentsMetadata Metadata { get; set; }
        }

        private class LegacyTableOfContentsItem : TableOfContentsItem
        {
            [JsonProperty(PropertyName = "homepage")]
            public string HomePage { get; set; }

            [JsonProperty(PropertyName = "children")]
            public new List<LegacyTableOfContentsItem> Children { get; set; }
        }
    }
}
