// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(Docset docset, Context context, string outputfolder)
        {
            foreach (var file in Directory.EnumerateFiles(outputfolder, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var relativeOutputFilePath = Path.GetRelativePath(outputfolder, file);

                var legacyOutputFilePathRelativeToSiteBasePath = relativeOutputFilePath;
                if (relativeOutputFilePath.StartsWith(docset.Config.SiteBasePath, StringComparison.Ordinal))
                {
                    legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, relativeOutputFilePath);
                }
                if (string.Equals(fileName, "toc.json", StringComparison.OrdinalIgnoreCase))
                {
                    var toc = JsonUtility.Deserialize<LegacyTableOfContentsModel>(File.ReadAllText(file));

                    var tocItemWithPath = toc?.Items?.FirstOrDefault();
                    if (tocItemWithPath != null)
                    {
                        tocItemWithPath.PdfAbsolutePath = PathUtility.NormalizeFile($"/{docset.Config.SiteBasePath}/opbuildpdf/{legacyOutputFilePathRelativeToSiteBasePath.ChangeExtension(".pdf")}");
                        tocItemWithPath.PdfName = PathUtility.NormalizeFile($"/{Path.GetDirectoryName(legacyOutputFilePathRelativeToSiteBasePath)}.pdf");
                    }

                    context.WriteJson(toc, relativeOutputFilePath);
                }
            }
        }

        private static string ChangeExtension(this string path, string extension)
        {
            return path.Substring(0, path.LastIndexOf('.')) + extension;
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
