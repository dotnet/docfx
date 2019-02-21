// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<(IEnumerable<Error> errors, object result, PublishItem publishItem)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, schema, model, metadata) = await Load(context, file, buildChild);

            if (!string.IsNullOrEmpty(metadata.BreadcrumbPath))
            {
                var (breadcrumbError, breadcrumbPath, _) = context.DependencyResolver.ResolveLink(metadata.BreadcrumbPath, file, file, buildChild);
                errors.AddIfNotNull(breadcrumbError);
                metadata.BreadcrumbPath = breadcrumbPath;
            }

            model.SchemaType = schema.Name;
            model.Locale = file.Docset.Locale;
            model.Metadata = metadata;
            model.OpenToPublicContributors = file.Docset.Config.Contribution.ShowEdit;
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = file.CanonicalUrl;
            model.Bilingual = file.Docset.Config.Localization.Bilingual;

            (model.DocumentId, model.DocumentVersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.OriginalContentGitUrlTemplate, model.Gitcommit) = await context.ContributionProvider.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, model.Author, model.Contributors, model.UpdatedAt) = await context.ContributionProvider.GetAuthorAndContributors(file, metadata.Author);
            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            var isPage = schema.Attribute is PageSchemaAttribute;
            var (output, outputPath, extensionData) = ApplyTemplate(context, file, model, isPage);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Path = outputPath,
                Locale = file.Docset.Locale,
                Monikers = model.Monikers,
                ExtensionData = extensionData,
            };

            return (errors, output, publishItem);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
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

        private static (List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)
            LoadMarkdown(Context context, Document file, Action<Document> buildChild)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(yamlHeaderErrors);

            var (metaErrors, fileMetadata) = context.MetadataProvider.GetMetadata<FileMetadata>(file, yamlHeader);
            errors.AddRange(metaErrors);

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, fileMetadata.MonikerRange);
            errors.AddIfNotNull(error);

            // TODO: handle blank page
            var (html, markup) = MarkdownUtility.ToHtml(
                content,
                file,
                context.DependencyResolver,
                buildChild,
                (rangeString) => context.MonikerProvider.GetZoneMonikers(rangeString, monikers, errors),
                MarkdownPipelineType.ConceptualMarkdown);
            errors.AddRange(markup.Errors);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);

            var model = new PageModel
            {
                Content = HtmlPostProcess(file, htmlDom),
                Title = yamlHeader.Value<string>("title") ?? HttpUtility.HtmlDecode(htmlTitleDom.InnerText),
                RawTitle = markup.HtmlTitle,
                WordCount = HtmlUtility.CountWord(htmlDom),
                Monikers = monikers,
            };

            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(htmlTitleDom)).ToHashSet();

            context.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, Schema.Conceptual, model, fileMetadata);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadYaml(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Deserialize(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadJson(Context context, Document file, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Deserialize(file, context);

            return await LoadSchemaDocument(context, errors, token, file, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadSchemaDocument(Context context, List<Error> errors, JToken token, Document file, Action<Document> buildChild)
        {
            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var obj = token as JObject;
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaViolationErrors, content) = JsonUtility.ToObjectWithSchemaValidation(token, schema.Type, transform: AttributeTransformer.TransformSDP(context, file, buildChild));
            errors.AddRange(schemaViolationErrors);

            if (file.Docset.Legacy && schema.Attribute is PageSchemaAttribute)
            {
                var html = await RazorTemplate.Render(schema.Name, content);
                content = HtmlPostProcess(file, HtmlUtility.LoadHtml(html));
            }

            // TODO: add check before to avoid case failure
            var yamlHeader = obj?.Value<JObject>("metadata") ?? new JObject();
            var title = yamlHeader.Value<string>("title") ?? obj?.Value<string>("title");

            var (metaErrors, fileMetadata) = context.MetadataProvider.GetMetadata<FileMetadata>(file, yamlHeader);
            errors.AddRange(metaErrors);

            var model = new PageModel
            {
                Content = content,
                Title = title,
                RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null,
                Monikers = new List<string>(),
            };

            return (errors, schema, model, fileMetadata);
        }

        private static string HtmlPostProcess(Document file, HtmlNode html)
        {
            html = html.StripTags();

            if (file.Docset.Legacy)
            {
                html = html.AddLinkType(file.Docset.Locale)
                           .RemoveRerunCodepenIframes();
            }

            return html.OuterHtml;
        }

        private static (object output, string outputPath, JObject extensionData) ApplyTemplate(
            Context context, Document file, PageModel model, bool isPage)
        {
            var outputPath = file.GetOutputPath(model.Monikers, isPage);

            if (!file.Docset.Config.Output.Json && !string.IsNullOrEmpty(file.Docset.Config.Theme))
            {
                return (file.Docset.Template.Render(model, file), outputPath, null);
            }

            if (file.Docset.Legacy)
            {
                if (isPage)
                {
                    var (output, extensionData) = TemplateTransform.Transform(model, file);
                    var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                    context.Output.WriteJson(extensionData, metadataPath);

                    return (output, outputPath, extensionData);
                }

                return (model, outputPath, null);
            }

            return (model, outputPath, isPage ? JsonUtility.ToJObject(model.Metadata) : null);
        }
    }
}
