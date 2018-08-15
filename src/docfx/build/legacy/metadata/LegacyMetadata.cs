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
        private static readonly string[] s_pageMetadataBlackList = { "_op_", "absolutePath", "canonical_url", "content_git_url", "open_to_public_contributors", "fileRelativePath", "layout", "title", "redirect_url", "contributors_to_exclude" };

        private static readonly string[] s_metadataBlackList = { "_op_", "fileRelativePath" };

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

        public static JObject GenerateLegacyRawMetadata(
            PageModel pageModel,
            string content,
            Docset docset,
            Document file,
            LegacyManifestOutput legacyManifestOutput,
            TableOfContentsMap tocMap)
        {
            var rawMetadata = pageModel.Metadata != null ? new JObject(pageModel.Metadata) : new JObject();

            rawMetadata = GenerataCommonMetadata(rawMetadata, docset);
            rawMetadata["conceptual"] = content;
            rawMetadata["fileRelativePath"] = legacyManifestOutput.PageOutput.OutputPathRelativeToSiteBasePath.Replace(".raw.page.json", ".html");
            rawMetadata["toc_rel"] = pageModel.TocRelativePath ?? tocMap.FindTocRelativePath(file);

            rawMetadata["wordCount"] = rawMetadata["word_count"] = pageModel.WordCount;

            rawMetadata["title"] = pageModel.Title;
            rawMetadata["rawTitle"] = pageModel.TitleHtml ?? "";

            rawMetadata["_op_canonicalUrlPrefix"] = $"{docset.Config.BaseUrl}/{docset.Config.Locale}/{docset.Config.SiteBasePath}/";

            if (docset.Config.NeedGeneratePdfUrlTemplate)
            {
                rawMetadata["_op_pdfUrlPrefixTemplate"] = $"{docset.Config.BaseUrl}/pdfstore/{pageModel.Locale}/{$"{docset.Config.Product}.{docset.Config.Name}"}/{{branchName}}";
            }

            rawMetadata["layout"] = rawMetadata.TryGetValue("layout", out JToken layout) ? layout : "Conceptual";

            rawMetadata["_path"] = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.Config.SiteBasePath, file.OutputPath));

            rawMetadata["document_id"] = pageModel.Id;
            rawMetadata["document_version_independent_id"] = pageModel.VersionIndependentId;

            if (!string.IsNullOrEmpty(pageModel.RedirectionUrl))
            {
                rawMetadata["redirect_url"] = pageModel.RedirectionUrl;
            }

            var culture = new CultureInfo(docset.Config.Locale);
            if (pageModel.UpdatedAt != default)
            {
                rawMetadata["_op_gitContributorInformation"] = new JObject
                {
                    ["author"] = pageModel.Author?.ToJObject(),
                    ["contributors"] = pageModel.Contributors != null
                        ? new JArray(pageModel.Contributors.Select(c => c.ToJObject()))
                        : null,
                    ["update_at"] = pageModel.UpdatedAt.ToString(culture.DateTimeFormat.ShortDatePattern, culture),
                };
            }
            if (!string.IsNullOrEmpty(pageModel.Author?.Name))
                rawMetadata["author"] = pageModel.Author?.Name;
            if (pageModel.UpdatedAt != default)
                rawMetadata["updated_at"] = pageModel.UpdatedAt.ToString("yyyy-MM-dd hh:mm tt", culture);

            rawMetadata["_op_openToPublicContributors"] = docset.Config.Contribution.Enabled;

            if (file.ContentType != ContentType.Redirection)
            {
                rawMetadata["open_to_public_contributors"] = docset.Config.Contribution.Enabled;

                if (!string.IsNullOrEmpty(pageModel.EditUrl))
                    rawMetadata["content_git_url"] = pageModel.EditUrl;

                if (!string.IsNullOrEmpty(pageModel.CommitUrl))
                    rawMetadata["gitcommit"] = pageModel.CommitUrl;
                if (!string.IsNullOrEmpty(pageModel.ContentUrl))
                    rawMetadata["original_content_git_url"] = pageModel.ContentUrl;
            }

            return rawMetadata;
        }

        public static string GenerateLegacyPageMetadata(JObject rawMetadata)
        {
            StringBuilder pageMetadataOutput = new StringBuilder(string.Empty);

            foreach (var item in rawMetadata)
            {
                if (!s_pageMetadataBlackList.Any(blackList => item.Key.StartsWith(blackList)))
                {
                    string content;
                    if (item.Value is JArray)
                    {
                        content = string.Join(",", item.Value);
                    }
                    else if (item.Value.Type == JTokenType.Boolean)
                    {
                        content = (bool)item.Value ? "true" : "false";
                    }
                    else
                    {
                        content = item.Value.ToString();
                    }
                    if (!string.IsNullOrEmpty(content))
                    {
                        pageMetadataOutput.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(item.Key)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
                    }
                }
            }

            return pageMetadataOutput.ToString();
        }

        public static JObject GenerateLegacyMetadateOutput(JObject rawMetadata)
        {
            var metadataOutput = new JObject();
            foreach (var item in rawMetadata)
            {
                if (!s_metadataBlackList.Any(blackList => item.Key.StartsWith(blackList)))
                {
                    metadataOutput[item.Key] = item.Value;
                }
            }

            metadataOutput["is_dynamic_rendering"] = true;

            return metadataOutput;
        }

        private static JObject ToJObject(this GitUserInfo info)
        {
            return new JObject
            {
                ["display_name"] = !string.IsNullOrEmpty(info.DisplayName) ? info.DisplayName : info.Name,
                ["id"] = info.Id,
                ["profile_url"] = info.ProfileUrl,
            };
        }
    }
}
