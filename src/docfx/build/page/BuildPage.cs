// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<(IEnumerable<Error> errors, object result, DependencyMap dependencies)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild,
            XrefMap xrefMap)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, schema, model, yamlHeader) = await Load(context, file, dependencies, bookmarkValidator, buildChild, xrefMap);
            var (metaErrors, metadata) = JsonUtility.ToObject<FileMetadata>(JsonUtility.Merge(Metadata.GetFromConfig(file), yamlHeader));
            errors.AddRange(metaErrors);

            model.PageType = schema.Name;
            model.Locale = file.Docset.Locale;
            model.Metadata = metadata;
            model.OpenToPublicContributors = file.Docset.Config.Contribution.ShowEdit;
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = GetCanonicalUrl(file);

            (model.DocumentId, model.DocumentVersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.Gitcommit) = contribution.GetGitUrls(file);

            List<Error> contributorErrors;
            (contributorErrors, model.Author, model.Contributors, model.UpdatedAt) = await contribution.GetAuthorAndContributors(file, metadata.Author);
            if (contributorErrors != null)
                errors.AddRange(contributorErrors);

            var output = (object)model;
            if (!file.Docset.Config.Output.Json && schema.Attribute is PageSchemaAttribute)
            {
                output = file.Docset.Legacy
                    ? file.Docset.LegacyTemplate.Render(model, file)
                    : await RazorTemplate.Render(model.PageType, model);
            }

            return (errors, output, dependencies.Build());
        }

        private static string GetCanonicalUrl(Document file)
        {
            var config = file.Docset.Config;
            var siteUrl = file.SiteUrl;
            if (file.IsExperimental)
            {
                var sitePath = ReplaceLast(file.SitePath, ".experimental", "");
                siteUrl = Document.PathToAbsoluteUrl(sitePath, file.ContentType, file.Schema, config.Output.Json);
            }

            return $"{config.BaseUrl}/{file.Docset.Locale}{siteUrl}";

            string ReplaceLast(string source, string find, string replace)
            {
                var i = source.LastIndexOf(find);
                return i >= 0 ? source.Remove(i, find.Length).Insert(i, replace) : source;
            }
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            Load(
            Context context, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(context, file, dependencies, bookmarkValidator, buildChild, xrefMap);
            }
            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(context, file, dependencies, bookmarkValidator, buildChild, xrefMap);
            }

            Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
            return await LoadJson(context, file, dependencies, bookmarkValidator, buildChild, xrefMap);
        }

        private static (List<Error> errors, Schema schema, PageModel model, JObject metadata)
            LoadMarkdown(
            Context context, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);
            var (html, markup) = Markup.ToHtml(content, file, new MarkdownPipelineCallback(xrefMap, dependencies, bookmarkValidator, buildChild), MarkdownPipelineType.ConceptualMarkdown);
            errors.AddRange(markup.Errors);
            var (metaErrors, metadata) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(metaErrors);
            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var title = metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(htmlTitleDom);
            var finalHtml = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);

            var model = new PageModel
            {
                Content = finalHtml,
                Title = title,
                RawTitle = markup.HtmlTitle,
                WordCount = wordCount,
            };

            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(htmlTitleDom)).ToHashSet();

            bookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, Schema.Conceptual, model, metadata);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            LoadYaml(
            Context context, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var (errors, token) = YamlUtility.Deserialize(file, context);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild, xrefMap);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            LoadJson(
            Context context, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var (errors, token) = JsonUtility.Deserialize(file, context);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild, xrefMap);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            LoadSchemaDocument(
            List<Error> errors, JToken token, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var obj = token as JObject;
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaViolationErrors, content) = JsonUtility.ToObject(token, schema.Type, transform: Transformer.Transform(errors, new MarkdownPipelineCallback(xrefMap, dependencies, bookmarkValidator, buildChild), file));
            errors.AddRange(schemaViolationErrors);

            if (file.Docset.Legacy && schema.Attribute is PageSchemaAttribute)
            {
                content = await RazorTemplate.Render(schema.Name, content);
            }

            // TODO: add check before to avoid case failure
            var metadata = obj?.Value<JObject>("metadata") ?? new JObject();
            var title = metadata.Value<string>("title") ?? obj?.Value<string>("title");

            var model = new PageModel
            {
                Content = content,
                Title = title,
                RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null,
            };

            return (errors, schema, model, metadata);
        }
    }
}
