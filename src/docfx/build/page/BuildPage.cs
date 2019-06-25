// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<IEnumerable<Error>> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, isPage, (pageMetadata, pageModel), (inputMetadata, metadataObject)) = await Load(context, file);
            var (generateErrors, outputMetadata) = await GenerateOutputMetadata(context, file, inputMetadata, pageMetadata);
            errors.AddRange(generateErrors);

            var outputPath = file.GetOutputPath(outputMetadata.Monikers, file.Docset.SiteBasePath, isPage);

            object output = null;
            JObject metadata = null;
            if (isPage)
            {
                var mergedOutputModel = new JObject();
                JsonUtility.Merge(mergedOutputModel, metadataObject);
                JsonUtility.Merge(mergedOutputModel, JsonUtility.ToJObject(outputMetadata));
                JsonUtility.Merge(mergedOutputModel, pageModel);
                (output, metadata) = ApplyPageTemplate(context, file, mergedOutputModel, pageModel.Value<string>("conceptual"));
            }
            else
            {
                // todo: support data page template
                output = pageModel;
                metadata = null;
            }

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath,
                Locale = file.Docset.Locale,
                Monikers = outputMetadata.Monikers,
                MonikerGroup = MonikerUtility.GetGroup(outputMetadata.Monikers),
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

                if (file.Docset.Legacy && metadata != null)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(metadata, metadataPath);
                }
            }

            if (Path.GetFileNameWithoutExtension(file.FilePath).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file.FilePath));
            }

            return errors;
        }

        private static async Task<(List<Error>, OutputMetadata)> GenerateOutputMetadata(
                Context context,
                Document file,
                InputMetadata inputMetadata,
                OutputMetadata outputMetadata)
        {
            var errors = new List<Error>();
            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveRelativeLink(file, inputMetadata.BreadcrumbPath, file);
                errors.AddIfNotNull(breadcrumbError);
                outputMetadata.BreadcrumbPath = breadcrumbPath;
            }

            outputMetadata.Locale = file.Docset.Locale;
            outputMetadata.TocRel = !string.IsNullOrEmpty(inputMetadata.TocRel) ? inputMetadata.TocRel : context.TocMap.FindTocRelativePath(file);
            outputMetadata.CanonicalUrl = file.CanonicalUrl;
            outputMetadata.EnableLocSxs = file.Docset.Config.Localization.Bilingual;
            outputMetadata.SiteName = file.Docset.Config.SiteName;

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);
            outputMetadata.Monikers = monikers;

            (outputMetadata.DocumentId, outputMetadata.DocumentVersionIndependentId) = context.BuildScope.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (outputMetadata.ContentGitUrl, outputMetadata.OriginalContentGitUrl, outputMetadata.OriginalContentGitUrlTemplate, outputMetadata.Gitcommit) = context.ContributionProvider.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, outputMetadata.ContributionInfo) = await context.ContributionProvider.GetContributionInfo(file, inputMetadata.Author);
            outputMetadata.Author = outputMetadata.ContributionInfo?.Author?.Name;
            outputMetadata.UpdatedAt = outputMetadata.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            outputMetadata.DepotName = $"{file.Docset.Config.Product}.{file.Docset.Config.Name}";
            outputMetadata.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            outputMetadata.CanonicalUrlPrefix = $"{file.Docset.HostName}/{outputMetadata.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                outputMetadata.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{outputMetadata.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            return (errors, outputMetadata);
        }

        private static async Task<(List<Error> errors, bool isPage, (OutputMetadata metadata, JObject model) output, (InputMetadata, JObject) inputMetadata)>
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

        private static (List<Error> errors, bool isPage, (OutputMetadata metadata, JObject model) output, (InputMetadata, JObject) inputMetadata)
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

            var pageMetadata = new OutputMetadata();

            pageMetadata.Title = inputMetadata.Title ?? title;
            pageMetadata.RawTitle = rawTitle;
            pageMetadata.SchemaType = "Conceptual";

            var pageModel = new JObject
            {
                ["conceptual"] = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture),
                ["wordCount"] = wordCount,
            };

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, true, (pageMetadata, pageModel), (inputMetadata, metadataObject));
        }

        private static async Task<(List<Error> errors, bool isPage, (OutputMetadata metadata, JObject model) output, (InputMetadata, JObject) inputMetadata)>
            LoadYaml(Context context, Document file)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, bool isPage, (OutputMetadata metadata, JObject model) output, (InputMetadata, JObject) inputMetadata)>
            LoadJson(Context context, Document file)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, bool isPage, (OutputMetadata metadata, JObject model) output, (InputMetadata, JObject) inputMetadata)>
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

            var pageMetadata = new OutputMetadata();
            var pageModel = transformedToken;

            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                // TODO: remove schema validation in ToObject
                var (_, content) = JsonUtility.ToObject(transformedToken, typeof(LandingData));

                // merge extension data to metadata in legacy model
                var landingData = (LandingData)content;
                JsonUtility.Merge(metadataObject, landingData.ExtensionData);

                pageModel = new JObject()
                {
                    ["conceptual"] = HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, content)).HtmlPostProcess(file.Docset.Culture),
                };
            }

            pageMetadata.Title = inputMetadata.Title ?? obj?.Value<string>("title");
            pageMetadata.RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null;
            pageMetadata.SchemaType = file.Mime;

            return (errors, !TemplateEngine.IsData(file.Mime), (pageMetadata, pageModel as JObject), (inputMetadata, metadataObject));
        }

        private static (object model, JObject metadata) ApplyPageTemplate(Context context, Document file, JObject output, string conceptual)
        {
            var rawMetadata = context.Template is null ? output : context.Template.CreateRawMetadata(output, file);

            if (!file.Docset.Config.Output.Json && context.Template != null)
            {
                return (context.Template.Render(conceptual, file, rawMetadata, file.Mime), null);
            }

            if (file.Docset.Legacy && context.Template != null)
            {
                return context.Template.Transform(conceptual, rawMetadata, file.Mime);
            }

            return (output, rawMetadata);
        }
    }
}
