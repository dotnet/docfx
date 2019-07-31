// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<List<Error>> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, sourceModel) = await Load(context, file);
            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);

            var outputPath = file.GetOutputPath(monikers, file.Docset.SiteBasePath, file.IsPage);

            var (inputMetaErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(inputMetaErrors);

            var (outputMetadataErrors, outputMetadata) = await CreateOutputMetadata(context, file, inputMetadata);
            errors.AddRange(outputMetadataErrors);

            var (output, metadata) = file.IsPage
                ? CreatePageOutput(context, file, sourceModel, inputMetadata, outputMetadata)
                : CreateDataOutput(context, file, sourceModel, inputMetadata, outputMetadata);

            if (Path.GetFileNameWithoutExtension(file.FilePath.Path).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file));
            }

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath.Path,
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

                if (file.Docset.Legacy && file.IsPage)
                {
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(metadata, metadataPath);
                }
            }

            return errors;
        }

        private static (object output, JObject metadata) CreatePageOutput(
            Context context,
            Document file,
            JObject sourceModel,
            InputMetadata inputMetadata,
            OutputMetadata outputMetadata)
        {
            var mergedMetadata = new JObject();
            JsonUtility.Merge(mergedMetadata, inputMetadata.RawJObject, JsonUtility.ToJObject(outputMetadata));

            var pageModel = new JObject();
            JsonUtility.Merge(pageModel, inputMetadata.RawJObject, sourceModel, JsonUtility.ToJObject(outputMetadata));

            if (file.Docset.Config.Output.Json && !file.Docset.Legacy)
            {
                return (pageModel, SortProperties(mergedMetadata));
            }

            var (templateModel, metadata) = CreateTemplateModel(context, SortProperties(pageModel), file);
            if (file.Docset.Config.Output.Json)
            {
                return (templateModel, SortProperties(metadata));
            }

            var html = context.TemplateEngine.RunLiquid(file, templateModel);
            return (html, SortProperties(metadata));

            JObject SortProperties(JObject obj)
                => new JObject(obj.Properties().OrderBy(p => p.Name));
        }

        private static (object output, JObject metadata)
            CreateDataOutput(Context context, Document file, JObject sourceModel, InputMetadata inputMetadata, OutputMetadata outputMetadata)
        {
            var mergedMetadata = new JObject();
            JsonUtility.Merge(mergedMetadata, inputMetadata.RawJObject);
            JsonUtility.Merge(mergedMetadata, JsonUtility.ToJObject(outputMetadata));
            sourceModel["metadata"] = mergedMetadata;
            return (context.TemplateEngine.RunJint($"{file.Mime}.json.js", sourceModel), null);
        }

        private static async Task<(List<Error>, OutputMetadata)> CreateOutputMetadata(Context context, Document file, InputMetadata inputMetadata)
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

            return (errors, pageModel);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadYaml(Context context, Document file)
        {
            var (errors, token) = YamlUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadJson(Context context, Document file)
        {
            var (errors, token) = JsonUtility.Parse(file, context);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, JObject model)>
            LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file)
        {
            var schemaTemplate = context.TemplateEngine.GetSchema(file.Mime);

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

            var pageModel = (JObject)transformedToken;
            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                var (metadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
                errors.AddRange(metadataErrors);

                var (deserializeErrors, landingData) = JsonUtility.ToObject<LandingData>(pageModel);
                errors.AddRange(deserializeErrors);

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, landingData)).HtmlPostProcess(file.Docset.Culture),
                    Title = inputMetadata.Title ?? obj?.Value<string>("title"),
                    RawTitle = $"<h1>{obj?.Value<string>("title")}</h1>",
                    ExtensionData = pageModel,
                });
            }

            return (errors, pageModel);
        }

        private static (TemplateModel model, JObject metadata) CreateTemplateModel(Context context, JObject pageModel, Document file)
        {
            var content = CreateContent(context, file, pageModel);

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var templateMetadata = context.TemplateEngine.RunJint(string.IsNullOrEmpty(file.Mime) ? "Conceptual.mta.json.js" : $"{file.Mime}.mta.json.js", pageModel);

            if (TemplateEngine.IsLandingData(file.Mime))
            {
                templateMetadata.Remove("conceptual");
            }

            // content for *.mta.json
            var metadata = new JObject(templateMetadata.Properties().Where(p => !p.Name.StartsWith("_")))
            {
                ["is_dynamic_rendering"] = true,
            };

            var pageMetadata = HtmlUtility.CreateHtmlMetaTags(metadata, context.MetadataProvider.HtmlMetaHidden, context.MetadataProvider.HtmlMetaNames);

            // content for *.raw.page.json
            var model = new TemplateModel
            {
                Content = content,
                RawMetadata = templateMetadata,
                PageMetadata = pageMetadata,
                ThemesRelativePathToOutputRoot = "_themes/",
            };

            return (model, metadata);
        }

        private static string CreateContent(Context context, Document file, JObject pageModel)
        {
            if (string.IsNullOrEmpty(file.Mime) || TemplateEngine.IsLandingData(file.Mime))
            {
                return pageModel.Value<string>("conceptual");
            }

            // Generate SDP content
            var jintResult = context.TemplateEngine.RunJint($"{file.Mime}.html.primary.js", pageModel);
            var content = context.TemplateEngine.RunMustache($"{file.Mime}.html.primary.tmpl", jintResult);

            var htmlDom = HtmlUtility.LoadHtml(content);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom);
            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return HtmlUtility.AddLinkType(htmlDom, file.Docset.Locale).WriteTo();
        }
    }
}
