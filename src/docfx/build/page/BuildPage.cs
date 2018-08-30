// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, pageType, content, fileMetadata) = await Load(file, dependencies, bookmarkValidator, buildChild);
            var conceptual = content as Conceptual;

            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), fileMetadata);
            var docId = file.Docset.Redirections.TryGetDocumentId(file, out var id) ? id : file.Id;

            // TODO: add check before to avoid case failure
            var (repoError, author, contributors, updatedAt) = await contribution.GetContributorInfo(file, metadata.Value<string>("author"));
            if (repoError != null)
                errors.Add(repoError);
            var (editUrl, contentUrl, commitUrl) = contribution.GetGitUrls(file);

            var title = metadata.Value<string>("title") ?? conceptual?.Title;

            // TODO: add toc spec test
            var toc = tocMap.FindTocRelativePath(file);

            var model = new PageModel
            {
                PageType = pageType,
                Content = conceptual?.Html ?? content,
                Metadata = metadata,
                Title = title,
                HtmlTitle = conceptual?.HtmlTitle,
                WordCount = conceptual?.WordCount ?? 0,
                Locale = locale,
                Toc = toc,
                Id = docId.id,
                VersionIndependentId = docId.versionIndependentId,
                Author = author,
                Contributors = contributors,
                UpdatedAt = updatedAt,
                EditUrl = editUrl,
                CommitUrl = commitUrl,
                ContentUrl = contentUrl,
                ShowEdit = file.Docset.Config.Contribution.ShowEdit,
            };

            return (errors, model, dependencies.Build());
        }

        private static async Task<(List<Error> errors, string pageType, object content, JObject metadata)>
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

        private static (List<Error> errors, string pageType, object content, JObject metadata)
            LoadMarkdown(
            Document file, string content, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (html, markup) = Markup.ToHtml(content, file, dependencies, bookmarkValidator, buildChild, MarkdownPipelineType.ConceptualMarkdown);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var title = HtmlUtility.GetInnerText(htmlTitleDom);
            var finalHtml = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var conceptual = new Conceptual { Html = finalHtml, WordCount = wordCount, HtmlTitle = markup.HtmlTitle, Title = title };

            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(htmlTitleDom)).ToHashSet();

            bookmarkValidator.AddBookmarks(file, bookmarks);

            return (markup.Errors, "Conceptual", conceptual, markup.Metadata);
        }

        private static async Task<(List<Error> errors, string pageType, object content, JObject metadata)>
            LoadYaml(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = YamlUtility.Deserialize(content);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static async Task<(List<Error> errors, string pageType, object content, JObject metadata)>
            LoadJson(
            string content, Document file, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (errors, token) = JsonUtility.Deserialize(content);

            return await LoadSchemaDocument(errors, token, file, dependencies, bookmarkValidator, buildChild);
        }

        private static async Task<(List<Error> errors, string pageType, object content, JObject metadata)>
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

            return (errors, schema.Name, content, metadata);

            object TransformContent(DataTypeAttribute attribute, JsonReader reader)
            {
                // Schema violation if the field is not what SchemaContentTypeAttribute required
                if (reader.ValueType != attribute.RequiredType)
                {
                    var lineInfo = reader as IJsonLineInfo;
                    var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                    errors.Add(Errors.ViolateSchema(range, $"Field with attribute '{attribute.GetType().ToString()}' should be of type {attribute.RequiredType.ToString()}."));
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

                // TODO: handle other attributes
                return reader.Value;
            }
        }
    }
}
