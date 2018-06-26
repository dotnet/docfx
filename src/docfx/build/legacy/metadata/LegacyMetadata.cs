// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;
using System.Linq;
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
            "locale", "ms.assetid", "ms.author", "ms.date", "ms.prod", "ms.topic", "original_content_git_url",
            "page_type", "pdf_url_template", "search.ms_docsetname", "search.ms_product", "search.ms_sitename", "site_name",
            "toc_rel", "uhfHeaderId", "updated_at", "version", "word_count",
        };

        private static readonly string[] s_metadataOutputItems =
        {
            "author", "breadcrumb_path", "canonical_url", "content_git_url", "depot_name", "description", "document_id",
            "document_version_independent_id", "experiment_id", "experimental", "gitcommit", "keywords",
            "layout", "locale", "ms.assetid", "ms.author", "ms.date", "ms.prod", "ms.topic", "open_to_public_contributors", "original_content_git_url",
            "page_type", "pdf_url_template", "search.ms_docsetname", "search.ms_product", "search.ms_sitename", "site_name", "title", "titleSuffix", "toc_asset_id",
            "toc_rel", "uhfHeaderId", "updated_at", "version", "word_count", "redirect_url",
        };

        public static JObject GenerataCommonMetadata(JObject metadata, Docset docset)
        {
            var newMetadata = new JObject(metadata);

            var depotName = $"{docset.Config.Product}.{docset.Config.Name}";
            newMetadata["depot_name"] = depotName;

            newMetadata["search.ms_docsetname"] = docset.Config.Name;
            newMetadata["search.ms_product"] = docset.Config.Product;
            newMetadata["search.ms_sitename"] = "Docs";

            newMetadata["locale"] = docset.Config.Locale;
            newMetadata["site_name"] = "Docs";
            newMetadata["version"] = 0;

            newMetadata["__global"] = new JObject
            {
                ["tutorial_allContributors"] = "all {0} contributors",
            };

            return newMetadata;
        }

        public static JObject GenerateLegacyRawMetadata(PageModel pageModel, Docset docset, Document file, GitRepoInfoProvider repo, TableOfContentsMap tocMap)
        {
            var rawMetadata = pageModel.Metadata != null ? new JObject(pageModel.Metadata) : new JObject();

            rawMetadata = GenerataCommonMetadata(rawMetadata, docset);
            rawMetadata["fileRelativePath"] = Path.GetFileNameWithoutExtension(file.OutputPath) + ".html";
            rawMetadata["toc_rel"] = pageModel.TocRelativePath ?? tocMap.FindTocRelativePath(file);

            rawMetadata["wordCount"] = rawMetadata["word_count"] = pageModel.WordCount;

            rawMetadata["rawTitle"] = !string.IsNullOrEmpty(pageModel.Title) ? $"<h1>{HttpUtility.HtmlEncode(pageModel.Title)}</h1>" : "";

            rawMetadata["_op_canonicalUrlPrefix"] = $"{docset.Config.BaseUrl}/{docset.Config.Locale}/{docset.Config.SiteBasePath}/";

            if (docset.Config.NeedGeneratePdfUrlTemplate)
            {
                rawMetadata["_op_pdfUrlPrefixTemplate"] = $"{docset.Config.BaseUrl}/pdfstore/{pageModel.Locale}/{$"{docset.Config.Product}.{docset.Config.Name}"}/{{branchName}}";
            }

            rawMetadata["layout"] = rawMetadata.TryGetValue("layout", out JToken layout) ? layout : "Conceptual";

            var path = PathUtility.NormalizeFile(file.ToLegacyPathRelativeToBasePath(docset));
            rawMetadata["_path"] = path.Remove(path.Length - Path.GetExtension(path).Length);

            rawMetadata["document_id"] = pageModel.Id;
            rawMetadata["document_version_independent_id"] = pageModel.VersionIndependentId;

            if (!string.IsNullOrEmpty(pageModel.RedirectionUrl))
            {
                rawMetadata["redirect_url"] = pageModel.RedirectionUrl;
            }

            var culture = new CultureInfo(pageModel.Locale);
            rawMetadata["_op_gitContributorInformation"] = new JObject
            {
                ["author"] = pageModel.Author?.ToJObject(),
                ["contributors"] = pageModel.Contributors != null
                    ? new JArray(pageModel.Contributors.Select(c => c.ToJObject()))
                    : null,
                ["update_at"] = pageModel.UpdatedAt.ToString(culture.DateTimeFormat.ShortDatePattern, culture),
            };
            rawMetadata["author"] = pageModel.Author?.ProfileUrl.Substring(pageModel.Author.ProfileUrl.LastIndexOf('/'));
            rawMetadata["updated_at"] = pageModel.UpdatedAt.ToString("yyyy-MM-dd hh:mm tt", culture);

            var repoInfo = repo.GetGitRepoInfo(file);
            if (repoInfo?.Host == GitHost.GitHub)
            {
                var fullPath = Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.FilePath));
                var relPath = PathUtility.NormalizeFile(Path.GetRelativePath(repoInfo.RootPath, fullPath));
                rawMetadata["original_content_git_url"] = $"https://github.com/{repoInfo.Account}/{repoInfo.Name}/blob/{repoInfo.Branch}/{relPath}";
                if (repo.TryGetCommits(file.FilePath, out var commits) && commits.Count > 0)
                {
                    rawMetadata["gitcommit"] = $"https://github.com/{repoInfo.Account}/{repoInfo.Name}/blob/{commits[0].Sha}/{relPath}";
                }
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

            metadataOutput["is_dynamic_rendering"] = true;

            return metadataOutput;
        }

        private static JObject ToJObject(this GitUserInfo info)
        {
            return new JObject
            {
                ["display_name"] = info.DisplayName,
                ["id"] = info.Id,
                ["profile_url"] = info.ProfileUrl,
            };
        }
    }
}
