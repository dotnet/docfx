// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static void Build(Context context, FilePath file)
        {
            var errors = context.ErrorBuilder;
            var sourceModel = Load(errors, context, file);
            if (context.ErrorBuilder.FileHasError(file))
            {
                return;
            }

            var isContentRenderType = context.DocumentProvider.GetRenderType(file) == RenderType.Content;

            var (output, metadata) = isContentRenderType
                ? CreatePageOutput(errors, context, file, sourceModel)
                : CreateDataOutput(context, file, sourceModel);

            var outputPath = context.DocumentProvider.GetOutputPath(file);

            if (!context.ErrorBuilder.FileHasError(file) && !context.Config.DryRun)
            {
                if (output is string str)
                {
                    context.Output.WriteText(outputPath, str);
                }
                else
                {
                    context.Output.WriteJson(outputPath, output);
                }

                if (context.Config.OutputType == OutputType.PageJson && isContentRenderType)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(metadataPath, metadata);
                }
            }

            context.PublishModelBuilder.SetPublishItem(file, metadata, outputPath);
        }

        private static (object output, JObject metadata) CreatePageOutput(
            ErrorBuilder errors, Context context, FilePath file, JObject sourceModel)
        {
            var outputMetadata = new JObject();
            var outputModel = new JObject();

            var mime = context.DocumentProvider.GetMime(file);
            var userMetadata = context.MetadataProvider.GetMetadata(errors, file);
            var systemMetadata = CreateSystemMetadata(errors, context, file, userMetadata);

            // Mandatory metadata are metadata that are required by template to successfully ran to completion.
            // The current bookmark validation for SDP validates against HTML produced from mustache,
            // so we need to run the full template for SDP even in --dry-run mode.
            if (context.Config.DryRun && TemplateEngine.IsConceptual(mime))
            {
                return (new JObject(), new JObject());
            }

            var systemMetadataJObject = JsonUtility.ToJObject(systemMetadata);

            if (TemplateEngine.IsConceptual(mime))
            {
                // conceptual raw metadata and raw model
                JsonUtility.Merge(outputMetadata, userMetadata.RawJObject, systemMetadataJObject);
                JsonUtility.Merge(outputModel, userMetadata.RawJObject, sourceModel, systemMetadataJObject);
            }
            else
            {
                JsonUtility.Merge(
                    outputMetadata,
                    sourceModel.TryGetValue<JObject>("metadata", out var sourceMetadata) ? sourceMetadata : new JObject(),
                    systemMetadataJObject);
                JsonUtility.Merge(outputModel, sourceModel, new JObject { ["metadata"] = outputMetadata });
            }

            if (context.Config.OutputType == OutputType.Json)
            {
                return (outputModel, JsonUtility.SortProperties(outputMetadata));
            }

            var (templateModel, templateMetadata) = CreateTemplateModel(context, file, mime, JsonUtility.SortProperties(outputModel));

            if (context.Config.OutputType == OutputType.PageJson)
            {
                return (templateModel, JsonUtility.SortProperties(templateMetadata));
            }

            try
            {
                var html = context.TemplateEngine.RunLiquid(mime, templateModel);
                return (html, JsonUtility.SortProperties(templateMetadata));
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex.Select(ex => ex.Error));
                return (templateModel, JsonUtility.SortProperties(templateMetadata));
            }
        }

        private static (object output, JObject metadata) CreateDataOutput(Context context, FilePath file, JObject sourceModel)
        {
            if (context.Config.DryRun)
            {
                return (new JObject(), new JObject());
            }

            var mime = context.DocumentProvider.GetMime(file);

            return (context.TemplateEngine.RunJavaScript($"{mime}.json.js", sourceModel), new JObject());
        }

        private static SystemMetadata CreateSystemMetadata(ErrorBuilder errors, Context context, FilePath file, UserMetadata userMetadata)
        {
            var systemMetadata = new SystemMetadata();

            if (!string.IsNullOrEmpty(userMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.LinkResolver.ResolveLink(
                    userMetadata.BreadcrumbPath,
                    userMetadata.BreadcrumbPath.Source is null ? file : userMetadata.BreadcrumbPath.Source.File,
                    file);
                errors.AddIfNotNull(breadcrumbError);
                systemMetadata.BreadcrumbPath = breadcrumbPath;
            }

            systemMetadata.Monikers = context.MonikerProvider.GetFileLevelMonikers(errors, file);

            if (IsCustomized404Page(file))
            {
                systemMetadata.Robots = "NOINDEX, NOFOLLOW";
                errors.Add(Errors.Content.Custom404Page(file));
            }

            systemMetadata.TocRel = !string.IsNullOrEmpty(userMetadata.TocRel) ? userMetadata.TocRel : context.TocMap.FindTocRelativePath(file);

            if (context.Config.DryRun)
            {
                return systemMetadata;
            }

            // To speed things up for dry runs, ignore metadata that does not produce errors.
            // We also ignore GitHub author validation for dry runs because we are not calling GitHub in local validation anyway.
            systemMetadata.ContributionInfo = context.ContributionProvider.GetContributionInfo(errors, file, userMetadata.Author);

            systemMetadata.Locale = context.BuildOptions.Locale;
            systemMetadata.CanonicalUrl = userMetadata.PageType != "profile" ? context.DocumentProvider.GetCanonicalUrl(file) : null;
            systemMetadata.Path = context.DocumentProvider.GetSitePath(file);
            systemMetadata.CanonicalUrlPrefix = UrlUtility.Combine($"https://{context.Config.HostName}", systemMetadata.Locale, context.Config.BasePath) + "/";

            systemMetadata.EnableLocSxs = context.BuildOptions.EnableSideBySide;
            systemMetadata.SiteName = context.Config.SiteName;

            (systemMetadata.DocumentId, systemMetadata.DocumentVersionIndependentId)
                = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file));

            (systemMetadata.ContentGitUrl, systemMetadata.OriginalContentGitUrl, systemMetadata.OriginalContentGitUrlTemplate)
                = userMetadata.ContentGitUrl != null || userMetadata.OriginalContentGitUrl != null || userMetadata.OriginalContentGitUrlTemplate != null
                  ? (userMetadata.ContentGitUrl, userMetadata.OriginalContentGitUrl, userMetadata.OriginalContentGitUrlTemplate)
                  : context.ContributionProvider.GetGitUrl(file);
            systemMetadata.Gitcommit = context.ContributionProvider.GetGitCommitUrl(file);

            systemMetadata.Author = systemMetadata.ContributionInfo?.Author?.Name;
            systemMetadata.UpdatedAt = systemMetadata.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            systemMetadata.SearchProduct = context.Config.Product;
            systemMetadata.SearchDocsetName = context.Config.Name;
            systemMetadata.SearchEngine = context.Config.SearchEngine;

            if (context.Config.OutputPdf)
            {
                systemMetadata.PdfUrlPrefixTemplate = UrlUtility.Combine(
                    $"https://{context.Config.HostName}", "pdfstore", systemMetadata.Locale, $"{context.Config.Product}.{context.Config.Name}", "{branchName}");
            }

            return systemMetadata;
        }

        private static JObject Load(ErrorBuilder errors, Context context, FilePath file)
        {
            return file.Format switch
            {
                FileFormat.Markdown => LoadMarkdown(errors, context, file),
                FileFormat.Yaml => LoadYaml(errors, context, file),
                FileFormat.Json => LoadJson(errors, context, file),
                _ => throw new InvalidOperationException(),
            };
        }

        private static JObject LoadMarkdown(ErrorBuilder errors, Context context, FilePath file)
        {
            var content = context.Input.ReadString(file);
            errors.AddIfNotNull(MergeConflict.CheckMergeConflictMarker(content, file));

            context.ContentValidator.ValidateSensitiveLanguage(file, content);

            var userMetadata = context.MetadataProvider.GetMetadata(errors, file);

            context.MetadataValidator.ValidateMetadata(errors, userMetadata.RawJObject, file);

            var conceptual = new ConceptualModel { Title = userMetadata.Title };
            var html = context.MarkdownEngine.ToHtml(errors, content, file, MarkdownPipelineType.Markdown, conceptual);

            context.SearchIndexBuilder.SetTitle(file, conceptual.Title);
            context.ContentValidator.ValidateTitle(file, conceptual.Title, userMetadata.TitleSuffix);

            ProcessConceptualHtml(conceptual, context, file, html);

            return context.Config.DryRun ? new JObject() : JsonUtility.ToJObject(conceptual);
        }

        private static JObject LoadYaml(ErrorBuilder errors, Context context, FilePath file)
        {
            return LoadSchemaDocument(errors, context, context.Input.ReadYaml(errors, file), file);
        }

        private static JObject LoadJson(ErrorBuilder errors, Context context, FilePath file)
        {
            return LoadSchemaDocument(errors, context, context.Input.ReadJson(errors, file), file);
        }

        private static JObject LoadSchemaDocument(ErrorBuilder errors, Context context, JToken token, FilePath file)
        {
            if (!(token is JObject obj))
            {
                throw Errors.JsonSchema.UnexpectedType(new SourceInfo(file, 1, 1), JTokenType.Object, token.Type).ToException();
            }

            // validate via json schema
            var mime = context.DocumentProvider.GetMime(file);
            var schemaValidator = context.TemplateEngine.GetSchemaValidator(mime);
            var schemaValidationErrors = schemaValidator.Validate(obj, file);
            errors.AddRange(schemaValidationErrors);

            var validatedObj = new JObject();
            JsonUtility.Merge(validatedObj, obj);

            // transform model via json schema
            if (context.DocumentProvider.GetRenderType(file) == RenderType.Content)
            {
                // transform metadata via json schema
                var userMetadata = context.MetadataProvider.GetMetadata(errors, file);
                JsonUtility.Merge(validatedObj, new JObject { ["metadata"] = userMetadata.RawJObject });

                context.MetadataValidator.ValidateMetadata(errors, userMetadata.RawJObject, file);
                context.SearchIndexBuilder.SetTitle(file, userMetadata.Title);
            }

            var schema = context.TemplateEngine.GetSchema(mime);
            var pageModel = (JObject)context.JsonSchemaTransformer.TransformContent(errors, schema, file, validatedObj);

            if (TemplateEngine.IsLandingData(mime))
            {
                var landingData = JsonUtility.ToObject<LandingData>(errors, pageModel);
                var razorHtml = RazorTemplate.Render(mime, landingData).GetAwaiter().GetResult();

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = ProcessHtml(context, file, razorHtml),
                    ExtensionData = pageModel,
                });
            }

            return pageModel;
        }

        private static (TemplateModel model, JObject metadata) CreateTemplateModel(Context context, FilePath file, string? mime, JObject pageModel)
        {
            var content = CreateContent(context, file, mime, pageModel);

            if (context.Config.DryRun)
            {
                return (new TemplateModel("", new JObject(), "", ""), new JObject());
            }

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var jsName = $"{mime}.mta.json.js";
            var templateMetadata = context.TemplateEngine.RunJavaScript(jsName, pageModel) as JObject ?? new JObject();

            if (TemplateEngine.IsLandingData(mime))
            {
                templateMetadata.Remove("conceptual");
            }

            // content for *.mta.json
            var metadata = new JObject(templateMetadata.Properties().Where(p => !p.Name.StartsWith("_")))
            {
                ["is_dynamic_rendering"] = true,
            };

            var pageMetadata = HtmlUtility.CreateHtmlMetaTags(
                metadata, context.MetadataProvider.HtmlMetaHidden, context.MetadataProvider.HtmlMetaNames);

            // content for *.raw.page.json
            var model = new TemplateModel(content, templateMetadata, pageMetadata, "_themes/");

            return (model, metadata);
        }

        private static string CreateContent(Context context, FilePath file, string? mime, JObject pageModel)
        {
            if (TemplateEngine.IsConceptual(mime) || TemplateEngine.IsLandingData(mime))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual");
            }

            // Generate SDP content
            var model = context.TemplateEngine.RunJavaScript($"{mime}.html.primary.js", pageModel);
            var content = context.TemplateEngine.RunMustache($"{mime}.html", model, file);

            return ProcessHtml(context, file, content);
        }

        private static string ProcessHtml(Context context, FilePath file, string html)
        {
            var bookmarks = new HashSet<string>();
            var searchText = new StringBuilder();

            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.GetBookmarks(ref token, bookmarks);
                HtmlUtility.AddLinkType(ref token, context.BuildOptions.Locale);

                if (token.Type == HtmlTokenType.Text)
                {
                    searchText.Append(token.RawText);
                }
            });

            context.BookmarkValidator.AddBookmarks(file, bookmarks);
            context.SearchIndexBuilder.SetBody(file, searchText.ToString());

            return LocalizationUtility.AddLeftToRightMarker(context.BuildOptions.Culture, result);
        }

        private static void ProcessConceptualHtml(ConceptualModel conceptual, Context context, FilePath file, string html)
        {
            var wordCount = 0L;
            var bookmarks = new HashSet<string>();
            var searchText = new StringBuilder();

            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.GetBookmarks(ref token, bookmarks);
                HtmlUtility.AddLinkType(ref token, context.BuildOptions.Locale);

                if (!context.Config.DryRun)
                {
                    HtmlUtility.CountWord(ref token, ref wordCount);
                }

                if (token.Type == HtmlTokenType.Text)
                {
                    searchText.Append(token.RawText);
                }
            });

            // Populate anchors from raw title
            if (!string.IsNullOrEmpty(conceptual.RawTitle))
            {
                var reader = new HtmlReader(conceptual.RawTitle);
                while (reader.Read(out var token))
                {
                    HtmlUtility.GetBookmarks(ref token, bookmarks);
                }
            }

            context.BookmarkValidator.AddBookmarks(file, bookmarks);
            context.SearchIndexBuilder.SetBody(file, searchText.ToString());

            conceptual.Conceptual = LocalizationUtility.AddLeftToRightMarker(context.BuildOptions.Culture, result);
            conceptual.WordCount = wordCount;
        }

        private static bool IsCustomized404Page(FilePath file)
        {
            return Path.GetFileNameWithoutExtension(file.Path).Equals("404", PathUtility.PathComparison);
        }
    }
}
