// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Web;

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMetadata
    {
        private static readonly string[] s_pageMetadataOutputItems =
       {
            "author", "breadcrumb_path", "depot_name", "description", "document_id",
            "document_version_independent_id", "gitcommit", "keywords",
            "locale", "ms.author", "ms.date", "ms.prod", "ms.topic", "original_content_git_url",
            "page_type", "search.ms_docsetname", "search.ms_product", "search.ms_sitename", "site_name",
            "toc_rel", "uhfHeaderId", "updated_at", "version", "word_count",
        };

        private static readonly string[] s_metadataOutputItems =
        {
            "author", "breadcrumb_path", "canonical_url", "content_git_url", "depot_name", "description", "document_id",
            "document_version_independent_id", "experiment_id", "experimental", "gitcommit", "is_dynamic_rendering", "keywords",
            "layout", "locale", "ms.author", "ms.date", "ms.prod", "ms.topic", "open_to_public_contributors", "original_content_git_url",
            "page_type", "search.ms_docsetname", "search.ms_product", "search.ms_sitename", "site_name", "title", "titleSuffix", "toc_asset_id",
            "toc_rel", "uhfHeaderId", "updated_at", "version", "word_count", "redirect_url",
        };

        public static JObject GenerateLegacyRawMetadata(PageModel pageModel, Docset docset, Document file, GitRepoInfoProvider repo)
        {
            var rawMetadata = pageModel.Metadata != null ? new JObject(pageModel.Metadata) : new JObject();
            rawMetadata["fileRelativePath"] = Path.GetFileNameWithoutExtension(file.OutputPath) + ".html";
            rawMetadata["toc_rel"] = pageModel.TocRelativePath;
            rawMetadata["locale"] = pageModel.Locale;
            rawMetadata["word_count"] = pageModel.WordCount;
            rawMetadata["depot_name"] = $"{docset.Config.Product}.{docset.Config.Name}";
            rawMetadata["site_name"] = "Docs";
            rawMetadata["version"] = 0;
            rawMetadata["_op_rawTitle"] = $"<h1>{HttpUtility.HtmlEncode(pageModel.Title ?? "")}</h1>";

            rawMetadata["_op_canonicalUrlPrefix"] = $"{docset.Config.BaseUrl}/{docset.Config.Locale}/{docset.Config.SiteBasePath}/";
            rawMetadata["_op_pdfUrlPrefixTemplate"] = $"{docset.Config.BaseUrl}/pdfstore/{pageModel.Locale}/{docset.Config.Name}/{{branchName}}{{pdfName}}";

            rawMetadata["_op_wordCount"] = pageModel.WordCount;

            rawMetadata["is_dynamic_rendering"] = true;
            rawMetadata["layout"] = rawMetadata.TryGetValue("layout", out JToken layout) ? layout : "Conceptual";

            rawMetadata["search.ms_docsetname"] = docset.Config.Name;
            rawMetadata["search.ms_product"] = docset.Config.Product;
            rawMetadata["search.ms_sitename"] = "Docs";

            rawMetadata["document_id"] = pageModel.Id;
            rawMetadata["document_version_independent_id"] = pageModel.VersionIndependentId;

            if (!string.IsNullOrEmpty(pageModel.RedirectionUrl))
            {
                rawMetadata["redirect_url"] = pageModel.RedirectionUrl;
                rawMetadata["redirect_document_id"] = true;
            }

            var repoInfo = repo.GetGitRepoInfo(file);
            if (repoInfo != null)
            {
                var fullPath = Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.FilePath));
                var relPath = PathUtility.NormalizeFile(Path.GetRelativePath(repoInfo.RootPath, fullPath));
                rawMetadata["gitcommit"] = repoInfo.GetGitPermaLink(relPath);
                rawMetadata["original_content_git_url"] = repoInfo.GetGitLink(relPath);
            }

            return rawMetadata;
        }

        public static string GenerateLegacyPageMetadata(JObject rawMetadata)
        {
            StringBuilder pageMetadataOutput = new StringBuilder(string.Empty);
            foreach (string item in s_pageMetadataOutputItems)
            {
                if (rawMetadata.TryGetValue(item, out JToken value))
                {
                    string content;
                    if (value is JArray)
                    {
                        content = string.Join(",", value);
                    }
                    else
                    {
                        content = value.ToString();
                    }
                    pageMetadataOutput.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(item)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
                }
            }

            return pageMetadataOutput.ToString();
        }

        public static JObject GenerateLegacyMetadateOutput(JObject rawMetadata)
        {
            var metadataOutput = new JObject();
            foreach (string item in s_metadataOutputItems)
            {
                if (rawMetadata.TryGetValue(item, out JToken value))
                {
                    metadataOutput[item] = value;
                }
            }

            return metadataOutput;
        }
    }
}
