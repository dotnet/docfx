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
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild,
            XrefMap xrefMap)
        {
            Error error;
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, schema, model, yamlHeader) = await Load(file, dependencies, bookmarkValidator, buildChild, xrefMap);
            var (metaErrors, metadata) = JsonUtility.ToObject<FileMetadata>(JsonUtility.Merge(Metadata.GetFromConfig(file), yamlHeader));
            errors.AddRange(metaErrors);

            model.PageType = schema.Name;
            model.Locale = file.Docset.Config.Locale;
            model.Metadata = metadata;
            model.OpenToPublicContributors = file.Docset.Config.Contribution.ShowEdit;
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = GetCanonicalUrl(file);

            (model.DocumentId, model.DocumentVersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.Gitcommit) = contribution.GetGitUrls(file);

            (error, model.Author, model.Contributors, model.UpdatedAt) = await contribution.GetContributorInfo(file, metadata.Author);
            if (error != null)
                errors.Add(error);

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

            return $"{config.BaseUrl}/{config.Locale}{siteUrl}";

            string ReplaceLast(string source, string find, string replace)
            {
                var i = source.LastIndexOf(find);
                return i >= 0 ? source.Remove(i, find.Length).Insert(i, replace) : source;
            }
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            Load(
            Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var content = file.ReadText();
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(file, content, dependencies, bookmarkValidator, buildChild, xrefMap);
            }
            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(content, file, dependencies, bookmarkValidator, buildChild, xrefMap);
            }
            Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
            return await LoadJson(content, file, dependencies, bookmarkValidator, buildChild, xrefMap);
        }

        private static (List<Error> errors, Schema schema, PageModel model, JObject metadata)
            LoadMarkdown(
            Document file, string content, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var (html, markup) = Markup.ToHtml(content, file, xrefMap, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.ConceptualMarkdown);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var title = markup.Metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(htmlTitleDom);
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

            return (markup.Errors, Schema.Conceptual, model, markup.Metadata);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            LoadYaml(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var (errors, token) = YamlUtility.Deserialize(content);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild, xrefMap);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, JObject metadata)>
            LoadJson(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild, XrefMap xrefMap)
        {
            var (errors, token) = JsonUtility.Deserialize(content);

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

            var (schemaViolationErrors, content) = JsonUtility.ToObject(token, schema.Type, transform: TransformContent);
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

            object TransformContent(DataTypeAttribute attribute, object value, string jsonPath)
            {
                if (attribute is HrefAttribute)
                {
                    return GetLink((string)value, file, file);
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, xrefMap, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)value, file, xrefMap, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is HtmlAttribute)
                {
                    var html = HtmlUtility.TransformLinks((string)value, href => GetLink(href, file, file));
                    return HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
                }

                if (attribute is XrefAttribute)
                {
                    // TODO: how to fill xref resolving data besides href
                    return xrefMap.Resolve((string)value).Href;
                }

                return value;

                string GetLink(string path, object relativeTo, object resultRelativeTo)
                {
                    Debug.Assert(relativeTo is Document);
                    Debug.Assert(resultRelativeTo is Document);

                    var self = (Document)relativeTo;

                    var (error, link, fragment, child) = self.TryResolveHref(path, (Document)resultRelativeTo);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    dependencies.AddDependencyItem(file, child, HrefUtility.FragmentToDependencyType(fragment));
                    return link;
                }
            }
        }
    }
}
