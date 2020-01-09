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

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddIfNotNull(monikerError);

            var (outputErrors, output, metadata) = file.IsPage
                ? await CreatePageOutput(context, file, sourceModel)
                : CreateDataOutput(context, file, sourceModel);
            errors.AddRange(outputErrors);

            if (Path.GetFileNameWithoutExtension(file.FilePath.Path).Equals("404", PathUtility.PathComparison))
            {
                // custom 404 page is not supported
                errors.Add(Errors.Custom404Page(file));
            }

            var outputPath = context.DocumentProvider.GetOutputPath(file.FilePath, monikers);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                SourcePath = file.FilePath.Path,
                Locale = file.Docset.Locale,
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
                ConfigMonikerRange = context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
                ExtensionData = metadata,
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && !context.Config.DryRun)
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
            Context context, Document file, JObject sourceModel)
        {
            var errors = new List<Error>();
            var outputMetadata = new JObject();
            var outputModel = new JObject();

            var (inputMetadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(inputMetadataErrors);
            var (systemMetadataErrors, systemMetadata) = await CreateSystemMetadata(context, file, userMetadata);
            errors.AddRange(systemMetadataErrors);

            // Mandatory metadata are metadata that are required by template to sucessfully ran to completion.
            // The current bookmark validation for SDP validates against HTML produced from mustache,
            // so we need to run the full template for SDP even in --dry-run mode.
            if (context.Config.DryRun && string.IsNullOrEmpty(file.Mime))
            {
                return (errors, null, new JObject());
            }

            var systemMetadataJObject = JsonUtility.ToJObject(systemMetadata);

            if (string.IsNullOrEmpty(file.Mime))
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

            JObject SortProperties(JObject obj) => new JObject(obj.Properties().OrderBy(p => p.Name));
        }

        private static (List<Error> errors, object output, JObject metadata)
            CreateDataOutput(Context context, Document file, JObject sourceModel)
        {
            if (context.Config.DryRun)
            {
                return (new List<Error>(), null, new JObject());
            }

            return (new List<Error>(), context.TemplateEngine.RunJint($"{file.Mime}.json.js", sourceModel), null);
        }

        private static async Task<(List<Error>, SystemMetadata)> CreateSystemMetadata(Context context, Document file, UserMetadata inputMetadata)
        {
            var errors = new List<Error>();
            var systemMetadata = new SystemMetadata();

            if (!string.IsNullOrEmpty(inputMetadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.LinkResolver.ResolveLink(
                    inputMetadata.BreadcrumbPath,
                    context.DocumentProvider.GetDocument(inputMetadata.BreadcrumbPath.Source.File),
                    file);
                errors.AddIfNotNull(breadcrumbError);
                systemMetadata.BreadcrumbPath = breadcrumbPath;
            }

            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddIfNotNull(monikerError);
            systemMetadata.Monikers = monikers;

            if (context.Config.DryRun)
            {
                return (errors, systemMetadata);
            }

            // To speed things up for dry runs, ignore metadatas that does not produce errors.
            // We also ignore GitHub author validation for dry runs because we are not calling GitHub in local validation anyway.
            var (contributorErrors, contributionInfo) = await context.ContributionProvider.GetContributionInfo(file, inputMetadata.Author);
            errors.AddRange(contributorErrors);
            systemMetadata.ContributionInfo = contributionInfo;

            systemMetadata.Locale = file.Docset.Locale;
            systemMetadata.CanonicalUrl = file.CanonicalUrl;
            systemMetadata.Path = file.SitePath;
            systemMetadata.CanonicalUrlPrefix = UrlUtility.Combine($"https://{file.Docset.Config.HostName}", systemMetadata.Locale, file.Docset.Config.BasePath.RelativePath) + "/";

            systemMetadata.TocRel = !string.IsNullOrEmpty(inputMetadata.TocRel)
                ? inputMetadata.TocRel : context.TocMap.FindTocRelativePath(file);
            systemMetadata.EnableLocSxs = context.LocalizationProvider.EnableSideBySide;
            systemMetadata.SiteName = file.Docset.Config.SiteName;

            (systemMetadata.DocumentId, systemMetadata.DocumentVersionIndependentId)
                = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file.FilePath));

            (systemMetadata.ContentGitUrl, systemMetadata.OriginalContentGitUrl, systemMetadata.OriginalContentGitUrlTemplate,
                systemMetadata.Gitcommit) = context.ContributionProvider.GetGitUrls(file);

            systemMetadata.Author = systemMetadata.ContributionInfo?.Author?.Name;
            systemMetadata.UpdatedAt = systemMetadata.ContributionInfo?.UpdatedAtDateTime.ToString("yyyy-MM-dd hh:mm tt");

            systemMetadata.SearchProduct = file.Docset.Config.Product;
            systemMetadata.SearchDocsetName = file.Docset.Config.Name;

            if (file.Docset.Config.Output.Pdf)
            {
                systemMetadata.PdfUrlPrefixTemplate = UrlUtility.Combine(
                    $"https://{file.Docset.Config.HostName}", "pdfstore", systemMetadata.Locale, $"{file.Docset.Config.Product}.{file.Docset.Config.Name}", "{branchName}");
            }

            return (errors, systemMetadata);
        }

        private static async Task<(List<Error> errors, JObject model)> Load(Context context, Document file)
        {
            if (file.FilePath.EndsWith(".md"))
            {
                return LoadMarkdown(context, file);
            }
            if (file.FilePath.EndsWith(".yml"))
            {
                return await LoadYaml(context, file);
            }

            Debug.Assert(file.FilePath.EndsWith(".json"));
            return await LoadJson(context, file);
        }

        private static (List<Error> errors, JObject model) LoadMarkdown(Context context, Document file)
        {
            var errors = new List<Error>();
            var content = context.Input.ReadString(file.FilePath);
            errors.AddIfNotNull(MergeConflict.CheckMergeConflictMarker(content, file.FilePath));

            var (markupErrors, html) = context.MarkdownEngine.ToHtml(content, file, MarkdownPipelineType.Markdown);
            errors.AddRange(markupErrors);

            var htmlDom = HtmlUtility.LoadHtml(html).PostMarkup(context.Config.DryRun);
            ValidateBookmarks(context, file, htmlDom);
            if (!HtmlUtility.TryExtractTitle(htmlDom, out var title, out var rawTitle))
            {
                errors.Add(Errors.HeadingNotFound(file));
            }

            var (metadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
            errors.AddRange(metadataErrors);

            if (context.Config.DryRun)
            {
                return (errors, new JObject());
            }

            var pageModel = JsonUtility.ToJObject(new ConceptualModel
            {
                Conceptual = CreateHtmlContent(file, htmlDom),
                WordCount = HtmlUtility.CountWord(htmlDom),
                RawTitle = rawTitle,
                Title = userMetadata.Title ?? title,
            });

            return (errors, pageModel);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadYaml(Context context, Document file)
        {
            var (errors, token) = context.Input.ReadYaml(file.FilePath);

            return await LoadSchemaDocument(context, errors, token, file);
        }

        private static async Task<(List<Error> errors, JObject model)> LoadJson(Context context, Document file)
        {
            var (errors, token) = context.Input.ReadJson(file.FilePath);

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

            var validatedObj = new JObject();
            JsonUtility.Merge(validatedObj, obj);

            // transform model via json schema
            if (file.IsPage)
            {
                // transform metadata via json schema
                var (metadataErrors, userMetadata) = context.MetadataProvider.GetMetadata(file.FilePath);
                JsonUtility.Merge(validatedObj, new JObject { ["metadata"] = userMetadata.RawJObject });
                errors.AddRange(metadataErrors);
            }

            var (schemaTransformError, transformedToken) = schemaTemplate.JsonSchemaTransformer.TransformContent(file, context, validatedObj);
            errors.AddRange(schemaTransformError);
            var pageModel = (JObject)transformedToken;

            if (file.Docset.Legacy && TemplateEngine.IsLandingData(file.Mime))
            {
                var (deserializeErrors, landingData) = JsonUtility.ToObject<LandingData>(pageModel);
                errors.AddRange(deserializeErrors);

                var htmlDom = HtmlUtility.LoadHtml(await RazorTemplate.Render(file.Mime, landingData)).PostMarkup(context.Config.DryRun);
                ValidateBookmarks(context, file, htmlDom);

                pageModel = JsonUtility.ToJObject(new ConceptualModel
                {
                    Conceptual = CreateHtmlContent(file, htmlDom),
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
                return (new TemplateModel(), new JObject());
            }

            // Hosting layers treats empty content as 404, so generate an empty <div></div>
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "<div></div>";
            }

            var templateMetadata = context.TemplateEngine.RunJint(
                string.IsNullOrEmpty(file.Mime) ? "Conceptual.mta.json.js" : $"{file.Mime}.mta.json.js", pageModel);

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
            var model = new TemplateModel
            {
                Content = content,
                RawMetadata = templateMetadata,
                PageMetadata = pageMetadata,
                ThemesRelativePathToOutputRoot = "_themes/",
            };

            return (model, metadata);
        }

        private static string CreateHtmlContent(Document file, HtmlNode html)
        {
            return LocalizationUtility.AddLeftToRightMarker(file.Docset.Culture, HtmlUtility.AddLinkType(html, file.Docset.Locale).WriteTo());
        }

        private static void ValidateBookmarks(Context context, Document file, HtmlNode html)
        {
            var bookmarks = HtmlUtility.GetBookmarks(html);
            context.BookmarkValidator.AddBookmarks(file, bookmarks);
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
            ValidateBookmarks(context, file, htmlDom);
            return CreateHtmlContent(file, htmlDom);
        }
    }
}
