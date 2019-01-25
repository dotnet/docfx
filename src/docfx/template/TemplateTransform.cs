// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TemplateTransform
    {
        private static readonly HashSet<string> s_metadataBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fileRelativePath",
        };

        private static readonly HashSet<string> s_htmlMetaTagsBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "absolutePath",
            "canonical_url",
            "content_git_url",
            "open_to_public_contributors",
            "original_content_git_url_template",
            "fileRelativePath",
            "layout",
            "title",
            "redirect_url",
            "contributors_to_exclude",
            "f1_keywords",
            "is_dynamic_rendering",
        };

        public static (TemplateModel model, JObject metadata) Transform(PageModel pageModel, Document file)
        {
            var rawMetadata = CreateRawMetadata(pageModel, file);
            var metadata = CreateMetadata(rawMetadata);
            var pageMetadata = CreateHtmlMetaTags(metadata);

            var model = new TemplateModel
            {
                Content = pageModel.Content as string,
                RawMetadata = rawMetadata,
                PageMetadata = pageMetadata,
                ThemesRelativePathToOutputRoot = "_themes/",
            };

            return (model, metadata);
        }

        private static JObject CreateRawMetadata(PageModel pageModel, Document file)
        {
            var docset = file.Docset;
            var rawMetadata = pageModel.Metadata != null ? JObject.FromObject(pageModel.Metadata, JsonUtility.DefaultSerializer) : new JObject();
            rawMetadata["depot_name"] = $"{docset.Config.Product}.{docset.Config.Name}";

            rawMetadata["search.ms_docsetname"] = docset.Config.Name;
            rawMetadata["search.ms_product"] = docset.Config.Product;
            rawMetadata["search.ms_sitename"] = "Docs";

            rawMetadata["locale"] = docset.Locale;
            rawMetadata["site_name"] = "Docs";

            rawMetadata["__global"] = docset.Template.Global;
            rawMetadata["conceptual"] = pageModel.Content as string;

            var path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.Config.DocumentId.SiteBasePath, file.SitePath));

            rawMetadata["_path"] = path;
            rawMetadata["toc_rel"] = pageModel.TocRel;

            rawMetadata["wordCount"] = rawMetadata["word_count"] = pageModel.WordCount;

            rawMetadata["title"] = pageModel.Title;
            rawMetadata["rawTitle"] = pageModel.RawTitle ?? "";

            rawMetadata["_op_canonicalUrlPrefix"] = $"{docset.Config.BaseUrl}/{docset.Locale}/{docset.Config.DocumentId.SiteBasePath}/";

            if (pageModel?.Monikers?.Count > 0)
            {
                rawMetadata["monikers"] = new JArray(pageModel.Monikers);
            }

            if (docset.Config.Output.Pdf)
            {
                rawMetadata["_op_pdfUrlPrefixTemplate"] = $"{docset.Config.BaseUrl}/pdfstore/{pageModel.Locale}/{docset.Config.Product}.{docset.Config.Name}/{{branchName}}";
            }

            rawMetadata["layout"] = rawMetadata.TryGetValue("layout", out JToken layout) ? layout : "Conceptual";

            rawMetadata["document_id"] = pageModel.DocumentId;
            rawMetadata["document_version_independent_id"] = pageModel.DocumentVersionIndependentId;

            if (!string.IsNullOrEmpty(pageModel.RedirectUrl))
            {
                rawMetadata["redirect_url"] = pageModel.RedirectUrl;
            }

            if (pageModel.UpdatedAt != default)
            {
                rawMetadata["_op_gitContributorInformation"] = new JObject
                {
                    ["author"] = pageModel.Author?.ToJObject(),
                    ["contributors"] = pageModel.Contributors != null
                        ? new JArray(pageModel.Contributors.Select(c => c.ToJObject()))
                        : null,
                    ["update_at"] = pageModel.UpdatedAt.ToString(docset.Culture.DateTimeFormat.ShortDatePattern),
                    ["updated_at_date_time"] = pageModel.UpdatedAt,
                };
            }
            if (!string.IsNullOrEmpty(pageModel.Author?.Name))
                rawMetadata["author"] = pageModel.Author?.Name;
            if (pageModel.UpdatedAt != default)
                rawMetadata["updated_at"] = pageModel.UpdatedAt.ToString("yyyy-MM-dd hh:mm tt");
            if (pageModel.Bilingual)
                rawMetadata["bilingual_type"] = "hover over";

            rawMetadata["_op_openToPublicContributors"] = docset.Config.Contribution.ShowEdit;
            rawMetadata["open_to_public_contributors"] = docset.Config.Contribution.ShowEdit;

            if (!string.IsNullOrEmpty(pageModel.ContentGitUrl))
                rawMetadata["content_git_url"] = pageModel.ContentGitUrl;
            if (!string.IsNullOrEmpty(pageModel.Gitcommit))
                rawMetadata["gitcommit"] = pageModel.Gitcommit;
            if (!string.IsNullOrEmpty(pageModel.OriginalContentGitUrl))
                rawMetadata["original_content_git_url"] = pageModel.OriginalContentGitUrl;
            if (!string.IsNullOrEmpty(pageModel.OriginalContentGitUrlTemplate))
                rawMetadata["original_content_git_url_template"] = pageModel.OriginalContentGitUrlTemplate;

            return RemoveUpdatedAtDateTime(
                TransformSchema(
                    docset.Template.TransformMetadata("Conceptual", rawMetadata), pageModel));
        }

        private static JObject TransformSchema(JObject metadata, PageModel model)
        {
            switch (model.SchemaType)
            {
                case "LandingData":
                    metadata["_op_layout"] = "LandingPage";
                    metadata["layout"] = "LandingPage";
                    metadata["page_type"] = "landingdata";

                    metadata.Remove("_op_gitContributorInformation");
                    metadata.Remove("_op_allContributorsStr");
                    break;

                case "Conceptual":
                case "ContextObject":
                    break;

                default:
                    throw new NotImplementedException($"Unknown page type {model.SchemaType}");
            }

            return metadata;
        }

        private static JObject CreateMetadata(JObject rawMetadata)
        {
            var metadata = new JObject();

            foreach (var (key, value) in rawMetadata)
            {
                if (!key.StartsWith("_op_") && !s_metadataBlacklist.Contains(key))
                {
                    metadata[key] = value;
                }
            }

            metadata["is_dynamic_rendering"] = true;

            return metadata;
        }

        private static string CreateHtmlMetaTags(JObject metadata)
        {
            var result = new StringBuilder();

            foreach (var (key, value) in metadata)
            {
                if (value is JObject || s_htmlMetaTagsBlacklist.Contains(key))
                {
                    continue;
                }

                var content = "";
                if (value is JArray arr)
                {
                    foreach (var v in value)
                    {
                        if (v is JValue)
                        {
                            result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(key)}\" content=\"{HttpUtility.HtmlEncode(v)}\" />");
                        }
                    }
                    continue;
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    content = (bool)value ? "true" : "false";
                }
                else
                {
                    content = value.ToString();
                }

                result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(key)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
            }

            return result.ToString();
        }

        private static JObject RemoveUpdatedAtDateTime(JObject rawMetadata)
        {
            JToken gitContributorInformation;
            if (rawMetadata.TryGetValue("_op_gitContributorInformation", out gitContributorInformation)
                && ((JObject)gitContributorInformation).ContainsKey("updated_at_date_time"))
            {
                ((JObject)rawMetadata["_op_gitContributorInformation"]).Remove("updated_at_date_time");
            }
            return rawMetadata;
        }

        private static JObject ToJObject(this Contributor info)
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
