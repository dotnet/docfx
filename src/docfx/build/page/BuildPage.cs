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
        private static HashSet<string> s_OutputModelPropertyNames = new HashSet<string>(JsonUtility.GetPropertyNames(typeof(OutputModel)));

        public static async Task<(IEnumerable<Error> errors, PublishItem publishItem)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, schema, model, inputMetadata) = await Load(context, file, buildChild);

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveLink(inputMetadata.BreadcrumbPath, file, file, buildChild);
                errors.AddIfNotNull(breadcrumbError);
                model.OverwriteMetadata("breadcrumb_path", breadcrumbPath);
            }

            model.SchemaType = schema.Name;
            model.OverwriteMetadata("locale", file.Docset.Locale);
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = file.CanonicalUrl;
            model.EnableLocSxs = file.Docset.Config.Localization.Bilingual;
            model.SiteName = file.Docset.Config.SiteName;

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);
            model.Monikers = monikers;

            (model.DocumentId, model.DocumentVersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.OriginalContentGitUrlTemplate, model.Gitcommit) = context.ContributionProvider.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, model.ContributionInfo) = await context.ContributionProvider.GetContributionInfo(file, inputMetadata.Author);
            model.OverwriteMetadata("author", new SourceInfo<string>(model.ContributionInfo?.Author?.Name, inputMetadata.Author), true);
            model.UpdatedAt = model.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            model.DepotName = $"{file.Docset.Config.Product}.{file.Docset.Config.Name}";
            model.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            model.CanonicalUrlPrefix = $"{file.Docset.HostName}/{file.Docset.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                model.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{file.Docset.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

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

        private static async Task<(List<Error> errors, Schema schema, OutputModel model, InputMetadata inputMetadata)>
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

        private static (List<Error> errors, Schema schema, OutputModel model, InputMetadata inputMetadata)
            LoadMarkdown(Context context, Document file, Action<Document> buildChild)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (markupErrors, html) = MarkdownUtility.ToHtml(
                content,
                file,
                context.DependencyResolver,
                buildChild,
                context.MonikerProvider,
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

            var (metadataErrors, metadataObject) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metadataErrors);

            var (validationErrors, pageModel, inputMetadata) = GetModels(metadataObject);
            errors.AddRange(validationErrors);

            pageModel.Conceptual = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture);
            pageModel.OverwriteMetadata("title", inputMetadata.Title ?? title);
            pageModel.RawTitle = rawTitle;
            pageModel.WordCount = wordCount;

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, Schema.Conceptual, pageModel, inputMetadata);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model, InputMetadata inputMetadata)>
            LoadYaml(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model, InputMetadata inputMetadata)>
            LoadJson(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, OutputModel model, InputMetadata inputMetadata)>
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

            var (metaErrors, metadataObject) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metaErrors);

            var (validationErrors, pageModel, inputMetadata) = GetModels(metadataObject);
            errors.AddRange(validationErrors);

            if (file.Docset.Legacy && file.Schema.Type == typeof(LandingData))
            {
                // merge extension data to metadata in legacy model
                var landingData = (LandingData)content;
                JsonUtility.Merge(pageModel.ExtensionData, landingData.ExtensionData);
            }

            if (file.Docset.Legacy && file.Schema.Attribute is PageSchemaAttribute)
            {
                pageModel.Conceptual = HtmlUtility.HtmlPostProcess(
                    await RazorTemplate.Render(file.Schema.Name, content), file.Docset.Culture);
            }
            else
            {
                pageModel.Content = content;
            }

            pageModel.OverwriteMetadata("title", inputMetadata.Title ?? obj?.Value<string>("title"));
            pageModel.RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null;

            return (errors, file.Schema, pageModel, inputMetadata);
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

        private static (List<Error>, OutputModel, InputMetadata) GetModels(JObject inputMetadata)
        {
            var (toObjectErrors, metadataModel) = JsonUtility.ToObject<InputMetadata>(inputMetadata);

            // todo: fix bug of extension data overwriting defined property
            foreach (var reservedName in s_OutputModelPropertyNames)
            {
                inputMetadata.Remove(reservedName);
            }
            return (toObjectErrors, new OutputModel { ExtensionData = inputMetadata }, metadataModel);
        }

        private static void OverwriteMetadata(this OutputModel outputModel, string key, string value, bool overwriteWhenExists = false)
        {
            if (value is null)
            {
                return;
            }

            if (outputModel.ExtensionData.TryGetValue(key, out var existingValue))
            {
                existingValue.Replace(new JValue(value));
            }
            else if (!overwriteWhenExists)
            {
                outputModel.ExtensionData[key] = value;
            }
        }
    }
}
