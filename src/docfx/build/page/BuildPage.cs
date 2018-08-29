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
        private static readonly Schema s_conceptual = Schema.GetSchema("Conceptual");

        public static async Task<(IEnumerable<Error> errors, PageModel result, DependencyMap dependencies)> Build(
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, schema, model) = Load(file, dependencies, bookmarkValidator, buildChild);

            model.PageType = schema.Name;
            model.Locale = file.Docset.Config.Locale;
            model.Metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), model.Metadata);
            model.ShowEdit = file.Docset.Config.Contribution.ShowEdit;

            if (schema.Attribute is PageSchemaAttribute pageSchema)
            {
                if (pageSchema.DocumentId)
                {
                    var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

                    model.Id = id;
                    model.VersionIndependentId = versionIndependentId;
                }

                if (pageSchema.Contributors)
                {
                    // TODO: add check before to avoid case failure
                    var authorName = model.Metadata.Value<string>("author");
                    var (error, author, contributors, updatedAt) = await contribution.GetContributorInfo(file, authorName);

                    if (error != null)
                        errors.Add(error);

                    model.Author = author;
                    model.Contributors = contributors;
                    model.UpdatedAt = updatedAt;
                }

                if (pageSchema.GitUrl)
                {
                    var (editUrl, contentUrl, commitUrl) = contribution.GetGitUrls(file);

                    model.EditUrl = editUrl;
                    model.ContentUrl = contentUrl;
                    model.CommitUrl = commitUrl;
                }

                if (pageSchema.Toc)
                {
                    // TODO: add toc spec test
                    model.Toc = tocMap.FindTocRelativePath(file);
                }
            }

            return (errors, model, dependencies.Build());
        }

        private static (List<Error> errors, Schema schema, PageModel model)
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
                return LoadYaml(content, file, dependencies, bookmarkValidator, buildChild);
            }
            else
            {
                Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
                return LoadJson(content, file, dependencies, bookmarkValidator, buildChild);
            }
        }

        private static (List<Error> errors, Schema schema, PageModel model)
            LoadMarkdown(
            Document file, string content, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (html, markup) = Markup.ToHtml(content, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.Markdown);

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

            return (markup.Errors, s_conceptual, model);
        }

        private static (List<Error> errors, Schema schema, PageModel model)
            LoadYaml(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Deserialize(content);

            return LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static (List<Error> errors, Schema schema, PageModel model)
            LoadJson(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Deserialize(content);

            return LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static (List<Error> errors, Schema schema, PageModel model)
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

            var metadata = obj?.Value<JObject>("metadata") ?? new JObject();
            var title = obj?.Value<string>("title") ?? metadata.Value<string>("title");

            var model = new PageModel
            {
                Content = content,
                Metadata = metadata,
                Title = title,
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
                    var (html, markup) = Markup.ToHtml(reader.Value.ToString(), file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.Markdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                if (attribute is InlineMarkdownAttribute)
                {
                    var (html, markup) = Markup.ToHtml(reader.Value.ToString(), file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.InlineMarkdown);
                    errors.AddRange(markup.Errors);
                    return html;
                }

                // TODO: handle other attributes
                return reader.Value;
            }
        }
    }
}
