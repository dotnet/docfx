// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<List<Error>> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, pageModel, inputMetadata) = await Load(context, file);
            var (generateErrors, outputMetadata) = await GenerateOutputMetadata(context, file, inputMetadata);
            errors.AddRange(generateErrors);

            var outputPath = file.GetOutputPath(outputMetadata.Monikers, file.Docset.SiteBasePath, file.IsPage);

            object output = null;
            JObject metadata = null;
            if (file.IsPage)
            {
                var mergedMetadata = new JObject();
                JsonUtility.Merge(mergedMetadata, inputMetadata.RawJObject);
                JsonUtility.Merge(mergedMetadata, JsonUtility.ToJObject(outputMetadata));

                var mergeModel = new JObject();
                JsonUtility.Merge(mergeModel, mergedMetadata);
                JsonUtility.Merge(mergeModel, pageModel);

                var isConceptual = string.IsNullOrEmpty(file.Mime) || TemplateEngine.IsLandingData(file.Mime);
                (output, metadata) = ApplyPageTemplate(context, file, mergedMetadata, mergeModel, isConceptual);
            }
            else
            {
                output = ApplyDataTemplate(context, file, pageModel);
                metadata = null;
            }

            if (Path.GetFileNameWithoutExtension(file.FilePath).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file.FilePath));
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

            return errors;
        }

        private static async Task<(List<Error>, OutputMetadata)> GenerateOutputMetadata(
                Context context,
                Document file,
                InputMetadata inputMetadata)
        {
            var errors = new List<Error>();
            var outputMetadata = new OutputMetadata();

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

            outputMetadata.SearchProduct = file.Docset.Config.Product;
            outputMetadata.SearchDocsetName = file.Docset.Config.Name;

            outputMetadata.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            outputMetadata.CanonicalUrlPrefix = $"{file.Docset.HostName}/{outputMetadata.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                outputMetadata.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{outputMetadata.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            return (errors, outputMetadata);
        }

        private static async Task<(List<Error> errors, JObject model, InputMetadata inputMetadata)>
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

        private static (List<Error> errors, JObject model, InputMetadata inputMetadata)
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

            var (metadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metadataErrors);

            var pageModel = JsonUtility.ToJObject(new ConceptualModel
            {
                Conceptual = HtmlUtility.HtmlPostProcess(htmlDom, file.Docset.Culture),
                WordCount = wordCount,
                RawTitle = rawTitle,
                Title = inputMetadata.Title ?? title,
            });

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, pageModel, inputMetadata);
        }

        private static async Task<(List<Error> errors, JObject model, InputMetadata inputMetadata)>
            LoadYaml(Context context, Document file)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, JObject model, InputMetadata inputMetadata)>
            LoadJson(Context context, Document file)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, JObject model, InputMetadata inputMetadata)>
            LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file)
        {
            var schemaTemplate = context.TemplateEngine.GetJsonSchema(file.Mime);

            if (!(token is JObject obj))
            {
                throw Errors.UnexpectedType(new SourceInfo(file.FilePath, 1, 1), JTokenType.Object, token.Type).ToException();
            }

            // validate via json schema
            var schemaValidationErrors = schemaTemplate.JsonSchemaValidator.Validate(obj);
            errors.AddRange(schemaValidationErrors);

            // transform via json schema
            var (schemaTransformError, transformedToken) = schemaTemplate.JsonSchemaTransformer.TransformContent(file, context, obj);
            errors.AddRange(schemaTransformError);

            var (metaErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metaErrors);

            var pageModel = (JObject)transformedToken;
            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                // TODO: remove schema validation in ToObject
                var (_, content) = JsonUtility.ToObject(transformedToken, typeof(LandingData));

                // merge extension data to metadata in legacy model
                var landingData = (LandingData)content;
                JsonUtility.Merge(inputMetadata.RawJObject, landingData.ExtensionData);

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, content)).HtmlPostProcess(file.Docset.Culture),
                    Title = inputMetadata.Title ?? obj?.Value<string>("title"),
                    RawTitle = $"<h1>{obj?.Value<string>("title")}</h1>",
                });
            }

            return (errors, pageModel, inputMetadata);
        }

        private static object ApplyDataTemplate(Context context, Document file, JObject pageModel)
        {
            return context.TemplateEngine.RunJintTransform($"{file.Mime}.json.js", pageModel);
        }

        private static (object model, JObject metadata) ApplyPageTemplate(Context context, Document file, JObject pageMetadata, JObject pageModel, bool isConceptual)
        {
            var conceptual = isConceptual ? pageModel.Value<string>("conceptual") : string.Empty;
            var processedMetadata = isConceptual ? pageModel : pageMetadata;

            if (!file.Docset.Config.Output.Json)
            {
                return (context.TemplateEngine.RunLiquid(conceptual, file, processedMetadata), null);
            }

            if (file.Docset.Legacy)
            {
                if (!isConceptual)
                {
                    // run sdp JINT and mustache to generate html
                    // conceptual = context.TemplateEngine.RenderMustache(file.Mime, pageModel);
                }

                return TransformToTemplateModel(context, conceptual, processedMetadata, file.Mime);
            }

            return (pageModel, processedMetadata);
        }

        private static (TemplateModel model, JObject metadata) TransformToTemplateModel(Context context, string conceptual, JObject rawMetadata, string mime)
        {
            // TODO: run page transform based on mime
            rawMetadata = context.TemplateEngine.RunJintTransform("Conceptual.mta.json.js", rawMetadata);
            if (TemplateEngine.IsLandingData(mime))
            {
                rawMetadata["_op_layout"] = "LandingPage";
                rawMetadata["layout"] = "LandingPage";
                rawMetadata["page_type"] = "landingdata";

                rawMetadata.Remove("_op_gitContributorInformation");
                rawMetadata.Remove("_op_allContributorsStr");
            }
            var metadata = TemplateEngine.CreateMetadata(rawMetadata);
            var pageMetadata = HtmlUtility.CreateHtmlMetaTags(metadata, context.TemplateEngine.HtmlMetaConfigs);

            var model = new TemplateModel
            {
                Content = conceptual,
                RawMetadata = rawMetadata,
                PageMetadata = pageMetadata,
                ThemesRelativePathToOutputRoot = "_themes/",
            };

            return (model, metadata);
        }
    }
}
