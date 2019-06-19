// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        private static HashSet<string> s_outputMetadatas = new HashSet<string>(JsonUtility.GetPropertyNames(typeof(OutputModel)));

        public static async Task<(IEnumerable<Error> errors, PublishItem publishItem)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, isPage, model, inputMetadata) = await Load(context, file);

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveLink(inputMetadata.BreadcrumbPath, file, file);
                errors.AddIfNotNull(breadcrumbError);
                model.BreadcrumbPath = breadcrumbPath;
            }

            model.Locale = file.Docset.Locale;
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
            model.Author = model.ContributionInfo?.Author?.Name;
            model.UpdatedAt = model.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            model.DepotName = $"{file.Docset.Config.Product}.{file.Docset.Config.Name}";
            model.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            model.CanonicalUrlPrefix = $"{file.Docset.HostName}/{model.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                model.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{model.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            var outputPath = file.GetOutputPath(model.Monikers, file.Docset.SiteBasePath, isPage);
            var (output, extensionData) = ApplyTemplate(context, file, model, isPage);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath,
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

        private static async Task<(List<Error> errors, bool isPage, OutputModel model, InputMetadata inputMetadata)>
            Load(Context context, Document file)
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

        private static (List<Error> errors, bool isPage, OutputModel model, InputMetadata inputMetadata)
            LoadMarkdown(Context context, Document file)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (markupErrors, html) = MarkdownUtility.ToHtml(
                context,
                content,
                file,
                MarkdownPipelineType.Markdown);
            errors.AddRange(markupErrors);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom);

            if (!HtmlUtility.TryExtractTitle(htmlDom, out var title, out var rawTitle))
            {
                errors.Add(Errors.HeadingNotFound(file));
            }

            var (metadataErrors, metadataObject, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metadataErrors);

            var pageModel = GetOutputModels(metadataObject);

            pageModel.Conceptual = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture);
            pageModel.Title = inputMetadata.Title ?? title;
            pageModel.RawTitle = rawTitle;
            pageModel.WordCount = wordCount;
            pageModel.SchemaType = "Conceptual";

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, true, pageModel, inputMetadata);
        }

        private static async Task<(List<Error> errors, bool isPage, OutputModel model, InputMetadata inputMetadata)>
            LoadYaml(Context context, Document file)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, bool isPage, OutputModel model, InputMetadata inputMetadata)>
            LoadJson(Context context, Document file)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, bool isPage, OutputModel model, InputMetadata inputMetadata)>
            LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file)
        {
            var obj = token as JObject;

            var (schemaValidator, schemaTransformer) = TemplateEngine.GetJsonSchema(file.Mime);
            if (schemaValidator is null || schemaTransformer is null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            // validate via json schema
            var schemaValidationErrors = schemaValidator.Validate(token);
            errors.AddRange(schemaValidationErrors);

            // transform via json schema
            var (schemaTransformError, transformedToken) = schemaTransformer.TransformContent(file, context, token);
            errors.AddRange(schemaTransformError);

            var (metaErrors, metadataObject, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metaErrors);

            var conceptual = (string)null;
            var pageModel = GetOutputModels(metadataObject);

            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                // TODO: remove schema validation in ToObject
                var (_, content) = JsonUtility.ToObject(transformedToken, typeof(LandingData));

                // merge extension data to metadata in legacy model
                var landingData = (LandingData)content;
                var mergedMetadata = new JObject();
                JsonUtility.Merge(mergedMetadata, metadataObject);
                JsonUtility.Merge(mergedMetadata, landingData.ExtensionData);

                (_, pageModel) = JsonUtility.ToObject<OutputModel>(mergedMetadata);

                if (file.Docset.Legacy)
                {
                    conceptual = HtmlUtility.HtmlPostProcess(
                    await RazorTemplate.Render(file.Mime, content), file.Docset.Culture);
                }
            }

            if (conceptual != null)
            {
                pageModel.Conceptual = conceptual;
            }
            else
            {
                pageModel.Content = transformedToken;
            }

            pageModel.Title = inputMetadata.Title ?? obj?.Value<string>("title");
            pageModel.RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null;
            pageModel.SchemaType = file.Mime;

            return (errors, !TemplateEngine.IsData(file.Mime), pageModel, inputMetadata);
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

        private static OutputModel GetOutputModels(JObject inputMetadata)
        {
            var clonedMetadata = (JObject)inputMetadata.DeepClone();

            // todo: fix extension data overwriting defined property
            clonedMetadata.Remove("monikerRange");
            foreach (var reserved in s_outputMetadatas)
            {
                clonedMetadata.Remove(reserved);
            }
            return new OutputModel { ExtensionData = clonedMetadata };
        }
    }
}
