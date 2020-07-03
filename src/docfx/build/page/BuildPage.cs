// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static void Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var errors = new List<Error>();

            var (loadErrors, sourceModel) = Load(context, file);
            errors.AddRange(loadErrors);

            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath);
            if (errors.Any(e => e.Level == ErrorLevel.Error))
            {
                context.ErrorLog.Write(errors);
                return;
            }

            var (outputErrors, output, metadata) = file.IsPage
                ? CreatePageOutput(context, file, sourceModel)
                : CreateDataOutput(context, file, sourceModel);
            errors.AddRange(outputErrors);
            context.ErrorLog.Write(errors);

            if (!context.ErrorLog.HasError(file.FilePath) && !context.Config.DryRun)
            {
                if (context.Config.OutputType == OutputType.Json)
                {
                    context.Output.WriteJson(outputPath, output);
                }
                else if (output is string str)
                {
                    context.Output.WriteText(outputPath, str);
                }
                else
                {
                    context.Output.WriteJson(Path.ChangeExtension(outputPath, ".json"), output);
                }

                if (context.Config.Legacy && file.IsPage)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(metadataPath, metadata);
                }
            }

            context.PublishModelBuilder.SetPublishItem(file.FilePath, metadata, outputPath);
        }

        private static (List<Error> errors, object output, JObject metadata) CreatePageOutput(
            Context context, Document file, JObject sourceModel)
        {
            var errors = new List<Error>();
            var outputMetadata = new JObject();
            var outputModel = new JObject();

            var (inputMetadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(inputMetadataErrors);
            var (systemMetadataErrors, systemMetadata) = CreateSystemMetadata(context, file, userMetadata);
            errors.AddRange(systemMetadataErrors);

            // Mandatory metadata are metadata that are required by template to successfully ran to completion.
            // The current bookmark validation for SDP validates against HTML produced from mustache,
            // so we need to run the full template for SDP even in --dry-run mode.
            if (context.Config.DryRun && TemplateEngine.IsConceptual(file.Mime) && context.Config.OutputType != OutputType.Html)
            {
                return (errors, new JObject(), new JObject());
            }

            var systemMetadataJObject = JsonUtility.ToJObject(systemMetadata);

            if (TemplateEngine.IsConceptual(file.Mime))
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

            if (context.Config.OutputType == OutputType.Json && !context.Config.Legacy)
            {
                return (errors, outputModel, JsonUtility.SortProperties(outputMetadata));
            }

            var (templateModel, templateMetadata) = CreateTemplateModel(context, JsonUtility.SortProperties(outputModel), file);
            if (context.Config.OutputType == OutputType.Json)
            {
                return (errors, templateModel, JsonUtility.SortProperties(templateMetadata));
            }

            try
            {
                var html = context.TemplateEngine.RunLiquid(file, templateModel);
                return (errors, html, JsonUtility.SortProperties(templateMetadata));
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex.Select(ex => ex.Error));
                return (errors, templateModel, JsonUtility.SortProperties(templateMetadata));
            }
        }

        private static (List<Error> errors, object output, JObject metadata) CreateDataOutput(Context context, Document file, JObject sourceModel)
        {
            if (context.Config.DryRun)
            {
                return (new List<Error>(), new JObject(), new JObject());
            }

            return (new List<Error>(), context.TemplateEngine.RunJavaScript($"{file.Mime}.json.js", sourceModel), new JObject());
        }

        private static (List<Error>, SystemMetadata) CreateSystemMetadata(Context context, Document file, UserMetadata inputMetadata)
        {
            var errors = new List<Error>();
            var systemMetadata = new SystemMetadata();

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.LinkResolver.ResolveLink(
                    inputMetadata.BreadcrumbPath,
                    inputMetadata.BreadcrumbPath.Source is null ? file : context.DocumentProvider.GetDocument(inputMetadata.BreadcrumbPath.Source.File),
                    file);
                errors.AddIfNotNull(breadcrumbError);
                systemMetadata.BreadcrumbPath = breadcrumbPath;
            }

            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);
            systemMetadata.Monikers = monikers;

            if (IsCustomized404Page(file))
            {
                systemMetadata.Robots = "NOINDEX, NOFOLLOW";
                errors.Add(Errors.Content.Custom404Page(file));
            }

            systemMetadata.TocRel = !string.IsNullOrEmpty(inputMetadata.TocRel)
                ? inputMetadata.TocRel : context.TocMap.FindTocRelativePath(file);

            if (context.Config.DryRun)
            {
                return (errors, systemMetadata);
            }

            // To speed things up for dry runs, ignore metadata that does not produce errors.
            // We also ignore GitHub author validation for dry runs because we are not calling GitHub in local validation anyway.
            var (contributorErrors, contributionInfo) = context.ContributionProvider.GetContributionInfo(file.FilePath, inputMetadata.Author);
            errors.AddRange(contributorErrors);
            systemMetadata.ContributionInfo = contributionInfo;

            systemMetadata.Locale = context.BuildOptions.Locale;
            systemMetadata.CanonicalUrl = file.CanonicalUrl;
            systemMetadata.Path = file.SitePath;
            systemMetadata.CanonicalUrlPrefix = UrlUtility.Combine($"https://{context.Config.HostName}", systemMetadata.Locale, context.Config.BasePath) + "/";

            systemMetadata.EnableLocSxs = context.BuildOptions.EnableSideBySide;
            systemMetadata.SiteName = context.Config.SiteName;

            (systemMetadata.DocumentId, systemMetadata.DocumentVersionIndependentId)
                = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file.FilePath));

            (systemMetadata.ContentGitUrl, systemMetadata.OriginalContentGitUrl, systemMetadata.OriginalContentGitUrlTemplate)
                = context.ContributionProvider.GetGitUrl(file.FilePath);
            systemMetadata.Gitcommit = context.ContributionProvider.GetGitCommitUrl(file.FilePath);

            systemMetadata.Author = systemMetadata.ContributionInfo?.Author?.Name;
            systemMetadata.UpdatedAt = systemMetadata.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            systemMetadata.SearchProduct = context.Config.Product;
            systemMetadata.SearchDocsetName = context.Config.Name;

            if (context.Config.OutputPdf)
            {
                systemMetadata.PdfUrlPrefixTemplate = UrlUtility.Combine(
                    $"https://{context.Config.HostName}", "pdfstore", systemMetadata.Locale, $"{context.Config.Product}.{context.Config.Name}", "{branchName}");
            }

            return (errors, systemMetadata);
        }

        private static (List<Error> errors, JObject model) Load(Context context, Document file)
        {
            return file.FilePath.Format switch
            {
                FileFormat.Markdown => LoadMarkdown(context, file),
                FileFormat.Yaml => LoadYaml(context, file),
                FileFormat.Json => LoadJson(context, file),
                _ => throw new InvalidOperationException(),
            };
        }

        private static (List<Error> errors, JObject model) LoadMarkdown(Context context, Document file)
        {
            var errors = new List<Error>();
            var content = context.Input.ReadString(file.FilePath);
            errors.AddIfNotNull(MergeConflict.CheckMergeConflictMarker(content, file.FilePath));

            var (metadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(metadataErrors);

            errors.AddRange(context.MetadataValidator.ValidateMetadata(userMetadata.RawJObject, file.FilePath));

            var (markupErrors, html) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.Markdown, out var contentTitle, out var rawTitle);
            errors.AddRange(markupErrors);

            var titleSuffix = userMetadata.RawJObject.TryGetValue<JValue>("titleSuffix", out var metadataValue) ? metadataValue.ToString() : default;
            context.ContentValidator.ValidateTitle(file, userMetadata.Title.Or(contentTitle), titleSuffix);

            var conceptual = new ConceptualModel { Title = userMetadata.Title.Or(contentTitle).Value, RawTitle = rawTitle };
            ProcessConceptualHtml(conceptual, context, file, html);

            return (errors, context.Config.DryRun ? new JObject() : JsonUtility.ToJObject(conceptual));
        }

        private static (List<Error> errors, JObject model) LoadYaml(Context context, Document file)
        {
            var (errors, token) = context.Input.ReadYaml(file.FilePath);

            return LoadSchemaDocument(context, errors, token, file);
        }

        private static (List<Error> errors, JObject model) LoadJson(Context context, Document file)
        {
            var (errors, token) = context.Input.ReadJson(file.FilePath);

            return LoadSchemaDocument(context, errors, token, file);
        }

        private static (List<Error> errors, JObject model) LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file)
        {
            var schemaTemplate = context.TemplateEngine.GetSchema(file.Mime);

            if (!(token is JObject obj))
            {
                throw Errors.JsonSchema.UnexpectedType(new SourceInfo(file.FilePath, 1, 1), JTokenType.Object, token.Type).ToException();
            }

            // validate via json schema
            var schemaValidationErrors = schemaTemplate.JsonSchemaValidator.Validate(obj);
            errors.AddRange(schemaValidationErrors);

            var validatedObj = new JObject();
            JsonUtility.Merge(validatedObj, obj);

            // transform model via json schema
            if (file.IsPage)
            {
                // transform metadata via json schema
                var (metadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
                JsonUtility.Merge(validatedObj, new JObject { ["metadata"] = userMetadata.RawJObject });
                errors.AddRange(metadataErrors);

                errors.AddRange(context.MetadataValidator.ValidateMetadata(userMetadata.RawJObject, file.FilePath));
            }

            var (schemaTransformError, transformedToken) = context.JsonSchemaTransformer.TransformContent(schemaTemplate.JsonSchema, file, validatedObj);
            errors.AddRange(schemaTransformError);
            var pageModel = (JObject)transformedToken;

            if (context.Config.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                var (deserializeErrors, landingData) = JsonUtility.ToObject<LandingData>(pageModel);
                errors.AddRange(deserializeErrors);

                var razorHtml = RazorTemplate.Render(file.Mime, landingData).GetAwaiter().GetResult();

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = ProcessHtml(context, file, razorHtml),
                    ExtensionData = pageModel,
                });
            }

            return (errors, pageModel);
        }

        private static (TemplateModel model, JObject metadata) CreateTemplateModel(Context context, JObject pageModel, Document file)
        {
            var content = CreateContent(context, file, pageModel);

            if (context.Config.DryRun)
            {
                return (new TemplateModel("", new JObject(), "", ""), new JObject());
            }

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var jsName = $"{file.Mime}.mta.json.js";
            var templateMetadata = context.TemplateEngine.RunJavaScript(jsName, pageModel) as JObject ?? new JObject();

            if (TemplateEngine.IsLandingData(file.Mime))
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

        private static string CreateContent(Context context, Document file, JObject pageModel)
        {
            if (TemplateEngine.IsConceptual(file.Mime) || TemplateEngine.IsLandingData(file.Mime))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual");
            }

            // Generate SDP content
            var model = context.TemplateEngine.RunJavaScript($"{file.Mime}.html.primary.js", pageModel);
            var content = context.TemplateEngine.RunMustache($"{file.Mime}.html.primary.tmpl", model, file.FilePath);

            return ProcessHtml(context, file, content);
        }

        private static string ProcessHtml(Context context, Document file, string html)
        {
            var bookmarks = new HashSet<string>();
            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.GetBookmarks(ref token, bookmarks);
                HtmlUtility.AddLinkType(ref token, context.BuildOptions.Locale);
            });

            context.BookmarkValidator.AddBookmarks(file, bookmarks);
            return LocalizationUtility.AddLeftToRightMarker(context.BuildOptions.Culture, result);
        }

        private static void ProcessConceptualHtml(ConceptualModel conceptual, Context context, Document file, string html)
        {
            var wordCount = 0L;
            var bookmarks = new HashSet<string>();
            var result = HtmlUtility.TransformHtml(html, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
            {
                HtmlUtility.GetBookmarks(ref token, bookmarks);
                HtmlUtility.AddLinkType(ref token, context.BuildOptions.Locale);

                if (!context.Config.DryRun)
                {
                    HtmlUtility.CountWord(ref token, ref wordCount);
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

            conceptual.Conceptual = LocalizationUtility.AddLeftToRightMarker(context.BuildOptions.Culture, result);
            conceptual.WordCount = wordCount;
        }

        private static bool IsCustomized404Page(Document file)
        {
            return Path.GetFileNameWithoutExtension(file.FilePath.Path).Equals("404", PathUtility.PathComparison);
        }
    }
}
