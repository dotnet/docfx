// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<List<Error>> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, model) = await Load(context, file);

            var (outputErrors, output, metadata) = file.IsData
                ? CreateDataOutput(model)
                : await CreatePageOutput(context, file, model);

            errors.AddRange(outputErrors);

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);

            if (Path.GetFileNameWithoutExtension(file.FilePath).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file.FilePath));
            }

            var outputPath = file.GetOutputPath(monikers, file.Docset.SiteBasePath, !file.IsData);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath,
                Locale = file.Docset.Locale,
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
                ExtensionData = metadata,
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem))
            {
                if (output is string str)
                {
                    context.Output.WriteText(str, publishItem.Path);
                }
                else
                {
                    context.Output.WriteJson(output, publishItem.Path);
                }

                if (file.Docset.Legacy && metadata.Count > 0)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(metadata, metadataPath);
                }
            }

            return errors;
        }

        private static (List<Error> errors, object output, JObject metadata)
            CreateDataOutput(JObject model)
        {
            // TODO: run jint
            return (new List<Error>(), model, new JObject());
        }

        private static async Task<(List<Error> errors, object output, JObject metadata)>
            CreatePageOutput(Context context, Document file, JObject model)
        {
            var errors = new List<Error>();

            var (inputMetadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(inputMetadataErrors);

            var (outputMetadataErrors, outputMetadata) = await CreateOutputMetadata(context, file, inputMetadata);
            var rawOutputMetadata = JsonUtility.ToJObject(outputMetadata);

            errors.AddRange(outputMetadataErrors);

            // Create page model
            var pageModel = new JObject();

            JsonUtility.Merge(pageModel, model);
            JsonUtility.Merge(pageModel, inputMetadata.RawObject);
            JsonUtility.Merge(pageModel, rawOutputMetadata);

            if (file.Docset.Config.Output.Json && !file.Docset.Legacy)
            {
                return (errors, pageModel, rawOutputMetadata);
            }

            // Create template model
            var (templateModel, metadata) = CreateTemplateModel(context, file, pageModel);

            if (file.Docset.Config.Output.Json)
            {
                return (errors, templateModel, metadata);
            }

            // Create HTML
            var html = context.TemplateEngine.RunLiquid(templateModel, file);

            return (errors, html, metadata);
        }

        private static async Task<(List<Error>, OutputMetadata)> CreateOutputMetadata(Context context, Document file, InputMetadata inputMetadata)
        {
            var errors = new List<Error>();
            var result = new OutputMetadata();

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveRelativeLink(file, inputMetadata.BreadcrumbPath, file);
                errors.AddIfNotNull(breadcrumbError);
                result.BreadcrumbPath = breadcrumbPath;
            }

            result.Locale = file.Docset.Locale;
            result.TocRel = !string.IsNullOrEmpty(inputMetadata.TocRel) ? inputMetadata.TocRel : context.TocMap.FindTocRelativePath(file);
            result.CanonicalUrl = file.CanonicalUrl;
            result.EnableLocSxs = file.Docset.Config.Localization.Bilingual;
            result.SiteName = file.Docset.Config.SiteName;

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);
            result.Monikers = monikers;

            (result.DocumentId, result.DocumentVersionIndependentId) = context.BuildScope.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (result.ContentGitUrl, result.OriginalContentGitUrl, result.OriginalContentGitUrlTemplate, result.Gitcommit) = context.ContributionProvider.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, result.ContributionInfo) = await context.ContributionProvider.GetContributionInfo(file, inputMetadata.Author);
            result.Author = result.ContributionInfo?.Author?.Name;
            result.UpdatedAt = result.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            result.DepotName = $"{file.Docset.Config.Product}.{file.Docset.Config.Name}";
            result.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            result.CanonicalUrlPrefix = $"{file.Docset.HostName}/{result.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                result.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{result.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            return (errors, result);
        }

        private static async Task<(List<Error> errors, JObject model)> Load(Context context, Document file)
        {
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(context, file);
            }
            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(context, file);
            }

            Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
            return await LoadJson(context, file);
        }

        private static (List<Error> errors, JObject model) LoadMarkdown(Context context, Document file)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (markupErrors, html) = MarkdownUtility.ToHtml(context, content, file, MarkdownPipelineType.Markdown);
            errors.AddRange(markupErrors);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom);

            if (!HtmlUtility.TryExtractTitle(htmlDom, out var title, out var rawTitle))
            {
                errors.Add(Errors.HeadingNotFound(file));
            }

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            var model = new JObject
            {
                ["conceptual"] = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture),
                ["wordCount"] = wordCount,
                ["rawTitle"] = rawTitle,
                ["title"] = title,
            };

            return (errors, model);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadYaml(Context context, Document file)
        {
            var (errors, token) = YamlUtility.Parse(file, context);
            var (schemaErrors, model) = await LoadSchemaDocument(context, token, file);
            errors.AddRange(schemaErrors);

            return (errors, model);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadJson(Context context, Document file)
        {
            var (errors, token) = JsonUtility.Parse(file, context);
            var (schemaErrors, model) = await LoadSchemaDocument(context, token, file);
            errors.AddRange(schemaErrors);

            return (errors, model);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadSchemaDocument(Context context, JToken token, Document file)
        {
            var (schemaValidator, schemaTransformer) = context.TemplateEngine.GetJsonSchema(file.Mime);
            if (schemaValidator is null || schemaTransformer is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var errors = new List<Error>();

            var schemaValidationErrors = schemaValidator.Validate(token);
            errors.AddRange(schemaValidationErrors);

            var (schemaTransformError, transformedToken) = schemaTransformer.TransformContent(file, context, token);
            errors.AddRange(schemaTransformError);

            if (!(transformedToken is JObject model))
            {
                throw Errors.UnexpectedType(new SourceInfo(file.FilePath), JTokenType.Object, token.Type).ToException();
            }

            if (TemplateEngine.IsLandingData(file.Mime))
            {
                var content = model.ToObject<LandingData>();
                var conceptual = HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, content)).HtmlPostProcess(file.Docset.Culture);

                model["rawTitle"] = $"<h1>{model?.Value<string>("title")}</h1>";
                model["conceptual"] = conceptual;
            }

            return (errors, model);
        }

        private static (TemplateModel, JObject metadata) CreateTemplateModel(Context context, Document file, JObject pageModel)
        {
            var isConceptual = string.IsNullOrEmpty(file.Mime) || TemplateEngine.IsLandingData(file.Mime);
            var conceptual = isConceptual ? pageModel.Value<string>("conceptual") : null;

            var rawMetadata = context.TemplateEngine.CreateRawMetadata(pageModel, file);

            return context.TemplateEngine.Transform(conceptual, rawMetadata, file.Mime);
        }
    }
}
