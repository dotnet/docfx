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
        public static async Task<(IEnumerable<Error> errors, PublishItem publishItem)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, schema, model) = await Load(context, file, buildChild);

            if (!string.IsNullOrEmpty(model.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveLink(model.BreadcrumbPath, file, file, buildChild);
                errors.AddIfNotNull(breadcrumbError);
                model.BreadcrumbPath.Value = breadcrumbPath;
            }

            model.SchemaType = schema.Name;
            model.Locale = file.Docset.Locale;
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = file.CanonicalUrl;
            model.EnableLocSxs = file.Docset.Config.Localization.Bilingual;
            model.SiteName = file.Docset.Config.SiteName;

            (model.DocumentId, model.DocumentVersionIndependentId) = context.BuildScope.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.OriginalContentGitUrlTemplate, model.Gitcommit) = context.ContributionProvider.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, model.ContributionInfo) = await context.ContributionProvider.GetContributionInfo(file, model.Author);
            model.Author = new SourceInfo<string>(model.ContributionInfo?.Author?.Name, model.Author);
            model.UpdatedAt = model.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            model.DepotName = $"{file.Docset.Config.Product}.{file.Docset.Config.Name}";
            model.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            model.CanonicalUrlPrefix = $"{file.Docset.HostName}/{file.Docset.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                model.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{model.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            var isPage = schema.Attribute is PageSchemaAttribute;
            var outputPath = file.GetOutputPath(model.Monikers, file.Docset.SiteBasePath, isPage);
            var (output, extensionData) = ApplyTemplate(context, file, model, isPage);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                Locale = file.Docset.Locale,
                Monikers = model.Monikers,
                MonikerGroup = MonikerUtility.GetGroup(model.Monikers),
                ExtensionData = extensionData,
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

                if (file.Docset.Legacy && extensionData != null)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(extensionData, metadataPath);
                }
            }

            if (Path.GetFileNameWithoutExtension(file.FilePath).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file.FilePath));
            }

            return (errors, publishItem);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model)>
            Load(Context context, Document file, Action<Document> buildChild)
        {
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(context, file, buildChild);
            }
            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(context, file, buildChild);
            }

            Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
            return await LoadJson(context, file, buildChild);
        }

        private static (List<Error> errors, Schema schema, OutputModel model)
            LoadMarkdown(Context context, Document file, Action<Document> buildChild)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(yamlHeaderErrors);

            var (metaErrors, pageModel) = context.MetadataProvider.GetInputMetadata<OutputModel>(file, yamlHeader);
            errors.AddRange(metaErrors);

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, pageModel.MonikerRange);
            errors.AddIfNotNull(error);

            // TODO: handle blank page
            var (markupErrors, html) = MarkdownUtility.ToHtml(
                content,
                file,
                context.DependencyResolver,
                buildChild,
                rangeString => context.MonikerProvider.GetZoneMonikers(rangeString, monikers, errors),
                key => context.Template?.GetToken(key),
                MarkdownPipelineType.Markdown);
            errors.AddRange(markupErrors);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom);

            if (!HtmlUtility.TryExtractTitle(htmlDom, out var title, out var rawTitle))
            {
                errors.Add(Errors.HeadingNotFound(file));
            }

            pageModel.Conceptual = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture);
            pageModel.Title = yamlHeader.Value<string>("title") ?? title;
            pageModel.RawTitle = rawTitle;
            pageModel.WordCount = wordCount;
            pageModel.Monikers = monikers;

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, Schema.Conceptual, pageModel);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model)>
            LoadYaml(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model)>
            LoadJson(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model)>
            LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file, Action<Document> buildChild)
        {
            var obj = token as JObject;
            if (file.Schema is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaValidator, schemaTransformer) = TemplateEngine.GetJsonSchema(file.Schema);
            if (schemaValidator is null || schemaTransformer is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            // validate via json schema
            var schemaValidationErrors = schemaValidator.Validate(token);
            errors.AddRange(schemaValidationErrors);

            // transform via json schema
            var (schemaTransformError, transformedToken) = schemaTransformer.TransformContent(file, context, token, buildChild);
            errors.AddRange(schemaTransformError);

            // TODO: remove schema validation in ToObject
            var (_, content) = JsonUtility.ToObject(transformedToken, file.Schema.Type);

            // TODO: add check before to avoid case failure
            var yamlHeader = obj?.Value<JObject>("metadata") ?? new JObject();
            if (file.Docset.Legacy && file.Schema.Type == typeof(LandingData))
            {
                // merge extension data to metadata in legacy model
                var landingData = (LandingData)content;
                var mergedMetadata = new JObject();
                JsonUtility.Merge(mergedMetadata, landingData.ExtensionData);
                JsonUtility.Merge(mergedMetadata, yamlHeader);
                yamlHeader = mergedMetadata;
            }
            var title = yamlHeader.Value<string>("title") ?? obj?.Value<string>("title");

            var (metaErrors, pageModel) = context.MetadataProvider.GetInputMetadata<OutputModel>(file, yamlHeader);
            errors.AddRange(metaErrors);

            if (file.Docset.Legacy && file.Schema.Attribute is PageSchemaAttribute)
            {
                pageModel.Conceptual = HtmlUtility.HtmlPostProcess(
                    await RazorTemplate.Render(file.Schema.Name, content), file.Docset.Culture);
            }
            else
            {
                pageModel.Content = content;
            }

            pageModel.Title = title;
            pageModel.RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null;
            pageModel.Monikers = new List<string>();

            return (errors, file.Schema, pageModel);
        }

        private static (object output, JObject extensionData) ApplyTemplate(Context context, Document file, OutputModel model, bool isPage)
        {
            var rawMetadata = context.Template is null ? model.ExtensionData : context.Template.CreateRawMetadata(model, file);

            if (!file.Docset.Config.Output.Json && context.Template != null)
            {
                return (context.Template.Render(model, file, rawMetadata), null);
            }

            if (file.Docset.Legacy)
            {
                if (isPage && context.Template != null)
                {
                    return context.Template.Transform(model, rawMetadata);
                }

                return (model, null);
            }

            return (model, isPage ? rawMetadata : null);
        }
    }
}
