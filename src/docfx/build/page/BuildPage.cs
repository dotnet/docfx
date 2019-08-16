// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<List<Error>> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var errors = new List<Error>();

            var (loadErrors, sourceModel) = await Load(context, file);
            errors.AddRange(loadErrors);

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);

            var outputPath = file.GetOutputPath(monikers, file.Docset.SiteBasePath, file.IsPage);

            var (outputErrors, output, metadata) = file.IsPage
                ? await CreatePageOutput(context, file, sourceModel)
                : CreateDataOutput(context, file, sourceModel);
            errors.AddRange(outputErrors);

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

        private static async Task<(List<Error> errors, object output, JObject metadata)> CreatePageOutput(
            Context context,
            Document file,
            JObject sourceModel)
        {
            var errors = new List<Error>();
            var outputMetadata = new JObject();
            var outputModel = new JObject();

            var (inputMetadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(inputMetadataErrors);
            var (systemMetadataErrors, systemMetadata) = await CreateSystemMetadata(context, file, inputMetadata);
            errors.AddRange(systemMetadataErrors);
            var systemMetadataJObject = JsonUtility.ToJObject(systemMetadata);

            if (string.IsNullOrEmpty(file.Mime))
            {
                // conceptual raw metadata and raw model
                JsonUtility.Merge(outputMetadata, inputMetadata.RawJObject, systemMetadataJObject);
                JsonUtility.Merge(outputModel, inputMetadata.RawJObject, sourceModel, systemMetadataJObject);
            }
            else
            {
                JsonUtility.Merge(outputMetadata, sourceModel.TryGetValue<JObject>("metadata", out var sourceMetadata) ? sourceMetadata : new JObject(), systemMetadataJObject);
                JsonUtility.Merge(outputModel, sourceModel, new JObject { ["metadata"] = outputMetadata });
            }

            if (file.Docset.Config.Output.Json && !file.Docset.Legacy)
            {
                return (errors, outputModel, SortProperties(outputMetadata));
            }

            var (templateModel, templateMetadata) = CreateTemplateModel(context, SortProperties(outputModel), file);
            if (file.Docset.Config.Output.Json)
            {
                return (errors, templateModel, SortProperties(templateMetadata));
            }

            var html = context.TemplateEngine.RunLiquid(file, templateModel);
            return (errors, html, SortProperties(templateMetadata));

            JObject SortProperties(JObject obj)
                => new JObject(obj.Properties().OrderBy(p => p.Name));
        }

        private static (List<Error> errors, object output, JObject metadata)
            CreateDataOutput(Context context, Document file, JObject sourceModel)
        {
            return (new List<Error>(), context.TemplateEngine.RunJint($"{file.Mime}.json.js", sourceModel), null);
        }

        private static async Task<(List<Error>, SystemMetadata)> CreateSystemMetadata(Context context, Document file, InputMetadata inputMetadata)
        {
            var errors = new List<Error>();
            var systemMetadata = new SystemMetadata();

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveRelativeLink(file, inputMetadata.BreadcrumbPath, file);
                errors.AddIfNotNull(breadcrumbError);
                systemMetadata.BreadcrumbPath = breadcrumbPath;
            }

            systemMetadata.Locale = file.Docset.Locale;
            systemMetadata.TocRel = !string.IsNullOrEmpty(inputMetadata.TocRel) ? inputMetadata.TocRel : context.TocMap.FindTocRelativePath(file);
            systemMetadata.CanonicalUrl = file.CanonicalUrl;
            systemMetadata.EnableLocSxs = file.Docset.Config.Localization.Bilingual;
            systemMetadata.SiteName = file.Docset.Config.SiteName;

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);
            systemMetadata.Monikers = monikers;

            (systemMetadata.DocumentId, systemMetadata.DocumentVersionIndependentId) = context.BuildScope.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (systemMetadata.ContentGitUrl, systemMetadata.OriginalContentGitUrl, systemMetadata.OriginalContentGitUrlTemplate, systemMetadata.Gitcommit) = context.ContributionProvider.GetGitUrls(context, file);

            List<Error> contributorErrors;
            (contributorErrors, systemMetadata.ContributionInfo) = await context.ContributionProvider.GetContributionInfo(context, file, inputMetadata.Author);
            systemMetadata.Author = systemMetadata.ContributionInfo?.Author?.Name;
            systemMetadata.UpdatedAt = systemMetadata.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            systemMetadata.SearchProduct = file.Docset.Config.Product;
            systemMetadata.SearchDocsetName = file.Docset.Config.Name;

            systemMetadata.Path = PathUtility.NormalizeFile(Path.GetRelativePath(file.Docset.SiteBasePath, file.SitePath));
            systemMetadata.CanonicalUrlPrefix = $"{file.Docset.HostName}/{systemMetadata.Locale}/{file.Docset.SiteBasePath}/";

            if (file.Docset.Config.Output.Pdf)
                systemMetadata.PdfUrlPrefixTemplate = $"{file.Docset.HostName}/pdfstore/{systemMetadata.Locale}/{file.Docset.Config.Product}.{file.Docset.Config.Name}/{{branchName}}";

            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            return (errors, systemMetadata);
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

            var (markupErrors, htmlDom) = MarkdownUtility.ToHtml(
                context,
                content,
                file,
                MarkdownPipelineType.Markdown);
            errors.AddRange(markupErrors);

            // get bookmarks before extracting title
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom);
            var wordCount = HtmlUtility.CountWord(htmlDom);

            if (!HtmlUtility.TryExtractTitle(htmlDom, out var title, out var rawTitle))
            {
                errors.Add(Errors.HeadingNotFound(file));
            }

            var (metadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
            errors.AddRange(metadataErrors);

            var pageModel = JsonUtility.ToJObject(new ConceptualModel
            {
                Conceptual = CreateHtmlContent(context, htmlDom, file, bookmarks),
                WordCount = wordCount,
                RawTitle = rawTitle,
                Title = inputMetadata.Title ?? title,
            });

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

            // transform model via json schema
            var (schemaTransformError, transformedToken) = schemaTemplate.JsonSchemaTransformer.TransformContent(file, context, obj);
            errors.AddRange(schemaTransformError);
            var pageModel = (JObject)transformedToken;

            if (file.IsPage)
            {
                // transform metadata via json schema
                var (metadataErrors, inputMetadata) = context.MetadataProvider.GetMetadata(file);
                var (metadataTransformedErrors, transformedMetadata) = schemaTemplate.JsonSchemaTransformer.TransformContent(file, context, new JObject { ["metadata"] = inputMetadata.RawJObject });
                errors.AddRange(metadataErrors);
                errors.AddRange(metadataTransformedErrors);
                pageModel["metadata"] = ((JObject)transformedMetadata)["metadata"];
            }

            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                var (deserializeErrors, landingData) = JsonUtility.ToObject<LandingData>(pageModel);
                errors.AddRange(deserializeErrors);

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = CreateHtmlContent(context, HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, landingData)).StripTags().RemoveRerunCodepenIframes(), file),
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

        private static string CreateHtmlContent(Context context, HtmlNode html, Document file, HashSet<string> bookmarks = null)
        {
            // add bookmark validation
            bookmarks = bookmarks ?? HtmlUtility.GetBookmarks(html);
            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            // get conceptual from htmldom
            return LocalizationUtility.AddLeftToRightMarker(file.Docset.Culture, HtmlUtility.AddLinkType(html, file.Docset.Locale).WriteTo());
        }

        private static string CreateContent(Context context, Document file, JObject pageModel)
        {
            if (string.IsNullOrEmpty(file.Mime) || TemplateEngine.IsLandingData(file.Mime))
            {
                // Conceptual and Landing Data
                return pageModel.Value<string>("conceptual");
            }

            // Generate SDP content
            var jintResult = context.TemplateEngine.RunJint($"{file.Mime}.html.primary.js", pageModel);
            var content = context.TemplateEngine.RunMustache($"{file.Mime}.html.primary.tmpl", jintResult);

            var htmlDom = HtmlUtility.LoadHtml(content);
            return CreateHtmlContent(context, htmlDom, file);
        }
    }
}
