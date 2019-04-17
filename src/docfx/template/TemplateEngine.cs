// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class TemplateEngine
    {
        private static readonly string[] s_resourceFolders = new[] { "global", "css", "fonts" };

        private static readonly HashSet<string> s_metadataBlacklist = new HashSet<string>()
        {
            "fileRelativePath",
        };

        private static readonly HashSet<string> s_htmlMetaTagsBlacklist = new HashSet<string>()
        {
            "absolutePath",
            "original_content_git_url_template",
            "fileRelativePath",
            "title",
            "internal_document_id",
            "product_family",
            "product_version",
            "redirect_url",
            "redirect_document_id",
            "toc_asset_id",
            "content_git_url",
            "open_to_public_contributors",
            "area",
            "theme",
            "theme_branch",
            "theme_url",
            "layout",
            "is_active",
            "api_scan",
            "publish_version",
            "canonical_url",
            "f1_keywords",
            "dev_langs",
            "is_dynamic_rendering",
            "helpviewer_keywords",
            "need_preview_pull_request",
            "contributors_to_exclude",
            "titleSuffix",
            "moniker_type",
            "is_significant_update",
            "archive_url",
            "serviceData",
            "is_hidden",
        };

        private static readonly Dictionary<string, string> s_displayNameMapping = new Dictionary<string, string>()
        {
            { "product", "Product" },
            { "topic_type", "TopicType" },
            { "api_type", "APIType" },
            { "api_location", "APILocation" },
            { "api_name", "APIName" },
            { "api_extra_info", "APIExtraInfo" },
            { "target_os", "TargetOS" },
        };

        private readonly string _templateDir;
        private readonly string _locale;
        private readonly LiquidTemplate _liquid;
        private readonly JavascriptEngine _js;

        public JObject Global { get; }

        private TemplateEngine(string templateDir, string locale)
        {
            var contentTemplateDir = Path.Combine(templateDir, "ContentTemplate");

            _templateDir = templateDir;
            _locale = locale.ToLowerInvariant();
            _liquid = new LiquidTemplate(templateDir);
            _js = new JavascriptEngine(contentTemplateDir);
            Global = LoadGlobalTokens(templateDir, _locale);
        }

        public static TemplateEngine Create(Docset docset)
        {
            Debug.Assert(docset != null);

            if (string.IsNullOrEmpty(docset.Config.Theme))
            {
                return null;
            }

            var (themeRemote, themeBranch) = LocalizationUtility.GetLocalizedTheme(docset.Config.Theme, docset.Locale, docset.Config.Localization.DefaultLocale);
            var (themePath, themeLock) = docset.RestoreMap.GetGitRestorePath($"{themeRemote}#{themeBranch}", docset.DependencyLock);
            Log.Write($"Using theme '{themeRemote}#{themeLock.Commit}' at '{themePath}'");

            return new TemplateEngine(themePath, docset.Locale);
        }

        public string Render(OutputModel model, Document file, JObject rawMetadata)
        {
            // TODO: only works for conceptual
            var content = model.Content.ToString();
            rawMetadata = TransformPageMetadata(rawMetadata, model);
            var metadata = CreateMetadata(rawMetadata);

            var layout = rawMetadata.Value<string>("layout");
            var themeRelativePath = PathUtility.GetRelativePathToFile(file.SitePath, "_themes");

            var liquidModel = new JObject
            {
                ["content"] = content,
                ["page"] = rawMetadata,
                ["metadata"] = metadata,
                ["theme_rel"] = themeRelativePath,
            };

            return _liquid.Render(layout, liquidModel);
        }

        public (TemplateModel model, JObject metadata) Transform(OutputModel pageModel, JObject rawMetadata)
        {
            rawMetadata = TransformPageMetadata(rawMetadata, pageModel);
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

        public void CopyTo(string outputPath)
        {
            foreach (var resourceDir in s_resourceFolders)
            {
                var srcDir = Path.Combine(_templateDir, resourceDir);
                if (Directory.Exists(srcDir))
                {
                    Parallel.ForEach(Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories), file =>
                    {
                        var outputFilePath = Path.Combine(outputPath, "_themes", file.Substring(_templateDir.Length + 1));
                        PathUtility.CreateDirectoryFromFilePath(outputFilePath);
                        File.Copy(file, outputFilePath, overwrite: true);
                    });
                }
            }
        }

        public string GetToken(string key)
        {
            return Global[key]?.ToString();
        }

        public JObject CreateRawMetadata(OutputModel pageModel, Document file)
        {
            var docset = file.Docset;
            var rawMetadata = JsonUtility.ToJObject(pageModel);

            rawMetadata["depot_name"] = $"{docset.Config.Product}.{docset.Config.Name}";

            rawMetadata["search.ms_docsetname"] = docset.Config.Name;
            rawMetadata["search.ms_product"] = docset.Config.Product;
            rawMetadata["search.ms_sitename"] = "Docs";

            rawMetadata["__global"] = Global;
            rawMetadata.Remove("content");

            var path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.Config.DocumentId.SiteBasePath, file.SitePath));

            rawMetadata["_path"] = path;
            rawMetadata["wordCount"] = pageModel.WordCount;

            rawMetadata["_op_canonicalUrlPrefix"] = $"{docset.Config.BaseUrl}/{docset.Locale}/{docset.Config.DocumentId.SiteBasePath}/";

            if (docset.Config.Output.Pdf)
            {
                rawMetadata["_op_pdfUrlPrefixTemplate"] = $"{docset.Config.BaseUrl}/pdfstore/{pageModel.Locale}/{docset.Config.Product}.{docset.Config.Name}/{{branchName}}";
            }

            rawMetadata.Remove("schema_type");

            return rawMetadata;
        }

        public JObject TransformTocMetadata(object model)
            => TransformMetadata("toc.json.js", JsonUtility.ToJObject(model));

        private JObject TransformPageMetadata(JObject rawMetadata, OutputModel pageModel)
        {
            return RemoveUpdatedAtDateTime(
                TransformSchema(
                    TransformMetadata("Conceptual.mta.json.js", rawMetadata), pageModel));
        }

        private JObject LoadGlobalTokens(string templateDir, string locale)
        {
            var path = Path.Combine(templateDir, $"LocalizedTokens/docs({locale}).html/tokens.json");
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
        }

        private JObject TransformMetadata(string scriptPath, JObject model)
        {
            return JObject.Parse(((JObject)_js.Run(scriptPath, "transform", model)).Value<string>("content"));
        }

        private static JObject TransformSchema(JObject metadata, OutputModel model)
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

            foreach (var property in metadata.Properties().OrderBy(p => p.Name))
            {
                var key = property.Name;
                var value = property.Value;
                if (value is JObject || s_htmlMetaTagsBlacklist.Contains(key))
                {
                    continue;
                }

                var name = s_displayNameMapping.TryGetValue(key, out var diplayName) ? diplayName : key;

                var content = "";
                if (value is JArray arr)
                {
                    foreach (var v in value)
                    {
                        if (v is JValue)
                        {
                            result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(name)}\" content=\"{HttpUtility.HtmlEncode(v)}\" />");
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

                result.AppendLine($"<meta name=\"{HttpUtility.HtmlEncode(name)}\" content=\"{HttpUtility.HtmlEncode(content)}\" />");
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

        private static JObject ToJObject(Contributor info)
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
