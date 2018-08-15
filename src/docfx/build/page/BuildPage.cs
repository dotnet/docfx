// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private static readonly Type[] s_schemaTypes = new[] { typeof(LandingData) };
        private static readonly IReadOnlyDictionary<string, Type> s_schemas = s_schemaTypes.ToDictionary(type => type.Name);

        public static async Task<(IEnumerable<Error> errors, PageModel result, DependencyMap dependencies)> Build(
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var dependencies = new DependencyMapBuilder();

            var (errors, pageType, content, htmlTitle, wordCount, fileMetadata) = Load(file, dependencies, bookmarkValidator, buildChild);

            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), fileMetadata);
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

            // TODO: add check before to avoid case failure
            var (repoErrors, author, contributors, updatedAt) = await contribution.GetContributorInfo(
                file,
                metadata.Value<string>("author"),
                metadata.Value<DateTime?>("update_date"));

            var (editUrl, contentUrl, commitUrl) = contribution.GetGitUrls(file);

            var title = metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(htmlTitle ?? "");

            // TODO: add toc spec test
            var toc = tocMap.FindTocRelativePath(file);

            var model = new PageModel
            {
                PageType = pageType,
                Content = content,
                Metadata = metadata,
                Title = title,
                HtmlTitle = htmlTitle,
                WordCount = wordCount,
                Locale = locale,
                Toc = toc,
                Id = id,
                VersionIndependentId = versionIndependentId,
                Author = author,
                Contributors = contributors,
                UpdatedAt = updatedAt,
                EditUrl = editUrl,
                CommitUrl = commitUrl,
                ContentUrl = contentUrl,
                EnableContribution = file.Docset.Config.Contribution.Enabled,
            };

            return (errors.Concat(repoErrors), model, dependencies.Build());
        }

        private static (List<Error> errors, string pageType, object content, string htmlTitle, long wordCount, JObject metadata)
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
                return LoadYaml(content);
            }
            else
            {
                Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
                return LoadJson(content);
            }
        }

        private static (List<Error> errors, string pageType, object content, string htmlTitle, long wordCount, JObject metadata)
            LoadMarkdown(
            Document file, string content, DependencyMapBuilder dependencies, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            var (html, markup) = Markup.ToHtml(content, file, dependencies, bookmarkValidator, buildChild);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var titleHtmlDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var model = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(titleHtmlDom)).ToHashSet();

            bookmarkValidator.AddBookmarks(file, bookmarks);

            return (markup.Errors, "Conceptual", model, markup.HtmlTitle, wordCount, markup.Metadata);
        }

        private static (List<Error> errors, string pageType, object content, string htmlTitle, long wordCount, JObject metadata)
            LoadYaml(string content)
        {
            var (errors, token) = YamlUtility.Deserialize(content);
            var schema = YamlUtility.ReadMime(content);
            if (schema == "YamlDocument")
            {
                schema = token.Value<string>("documentType");
            }

            return LoadSchemaDocument(errors, token, schema);
        }

        private static (List<Error> errors, string pageType, object content, string htmlTitle, long wordCount, JObject metadata)
            LoadJson(string content)
        {
            var (errors, token) = JsonUtility.Deserialize(content);
            var schemaUrl = token.Value<string>("$schema");

            // TODO: be more strict
            var schema = schemaUrl.Split('/').LastOrDefault();
            if (schema != null)
            {
                schema = Path.GetFileNameWithoutExtension(schema);
            }

            return LoadSchemaDocument(errors, token, schema);
        }

        private static (List<Error> errors, string pageType, object content, string htmlTitle, long wordCount, JObject metadata)
            LoadSchemaDocument(
            List<Error> errors, JToken token, string schema)
        {
            if (schema == null || !s_schemas.TryGetValue(schema, out var schemaType))
            {
                throw Errors.SchemaNotFound(schema).ToException();
            }

            var content = token.ToObject(schemaType);

            return (errors, schema, content, null, 0, token.Value<JObject>("metadata"));
        }
    }
}
