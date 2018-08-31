// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<(IEnumerable<Error> errors, PageModel result, DependencyMap dependencies)> Build(
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            Error error;
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, schema, model) = await Load(file, dependencies, bookmarkValidator, buildChild);
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), model.Metadata);

            model.PageType = schema.Name;
            model.Locale = file.Docset.Config.Locale;
            model.Metadata = metadata;
            model.ShowEdit = file.Docset.Config.Contribution.ShowEdit;
            model.Toc = tocMap.FindTocRelativePath(file);

            (model.Id, model.VersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.EditUrl, model.ContentUrl, model.CommitUrl) = contribution.GetGitUrls(file);

            // TODO: add check before to avoid case failure
            var authorName = metadata.Value<string>("author");
            (error, model.Author, model.Contributors, model.UpdatedAt) = await contribution.GetContributorInfo(file, authorName);
            if (error != null)
                errors.Add(error);

            return (errors, model, dependencies.Build());
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model)>
            Load(
            Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var content = file.ReadText();
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(file, content, dependencies, bookmarkValidator, buildChild);
            }
            else if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(content, file, dependencies, bookmarkValidator, buildChild);
            }
            else
            {
                Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
                return await LoadJson(content, file, dependencies, bookmarkValidator, buildChild);
            }
        }

        private static (List<Error> errors, Schema schema, PageModel model)
            LoadMarkdown(
            Document file, string content, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (html, markup) = Markup.ToHtml(content, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.ConceptualMarkdown);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var title = markup.Metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(htmlTitleDom);
            var finalHtml = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);

            var model = new PageModel
            {
                Content = finalHtml,
                Metadata = markup.Metadata,
                Title = title,
                HtmlTitle = markup.HtmlTitle,
                WordCount = wordCount,
            };

            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(htmlTitleDom)).ToHashSet();

            bookmarkValidator.AddBookmarks(file, bookmarks);

            return (markup.Errors, Schema.Conceptual, model);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model)>
            LoadYaml(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Deserialize(content);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model)>
            LoadJson(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Deserialize(content);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model)>
            LoadSchemaDocument(
            List<Error> errors, JToken token, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
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
                content = await Template.Render(schema.Name, content);
            }

            var metadata = obj?.Value<JObject>("metadata") ?? new JObject();
            var title = metadata.Value<string>("title");

            var model = new PageModel
            {
                Content = content,
                Metadata = metadata,
                Title = title,
                HtmlTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null,
            };

            return (errors, schema, model);

            object TransformContent(DataTypeAttribute attribute, JsonReader reader)
            {
                // Schema violation if the field is not what SchemaContentTypeAttribute required
                if (reader.ValueType != attribute.TargetType)
                {
                    var lineInfo = reader as IJsonLineInfo;
                    var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                    errors.Add(Errors.ViolateSchema(range, $"Field with attribute '{attribute.GetType()}' should be of type {attribute.TargetType}."));
                    return null;
                }

                if (attribute is HrefAttribute)
                {
                    var (error, href, fragment, doc) = Resolve.TryResolveHref(file, reader.Value.ToString(), file);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    return href;
                }

                if (attribute is MarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)reader.Value, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)reader.Value, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is HtmlAttribute)
                {
                    var (html, markup) = Markup.ToHtml((string)reader.Value, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.Html);
                    errors.AddRange(markup.Errors);
                    return HtmlUtility.StripTags(HtmlUtility.LoadHtml(html));
                }

                // TODO: handle other attributes
                return reader.Value;
            }
        }
    }
}
