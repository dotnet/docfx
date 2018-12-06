// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildPage
    {
        public static async Task<(IEnumerable<Error> errors, object result, List<string>)> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionProvider contribution,
            PageCallback callback,
            GitCommitProvider gitCommitProvider)
        {
            Debug.Assert(file.ContentType == ContentType.Page);

            var (errors, schema, model, metadata) = await Load(context, file, callback, gitCommitProvider);

            model.PageType = schema.Name;
            model.Locale = file.Docset.Locale;
            model.Metadata = metadata;
            model.OpenToPublicContributors = file.Docset.Config.Contribution.ShowEdit;
            model.TocRel = tocMap.FindTocRelativePath(file);
            model.CanonicalUrl = GetCanonicalUrl(file);
            model.Bilingual = file.Docset.Config.Localization.Bilingual;

            (model.DocumentId, model.DocumentVersionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            (model.ContentGitUrl, model.OriginalContentGitUrl, model.Gitcommit) = await contribution.GetGitUrls(file);

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

            return (errors, output, model.Monikers);
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

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            Load(
            Context context, Document file, PageCallback callback, GitCommitProvider gitCommitProvider)
        {
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                return LoadMarkdown(context, file, callback, gitCommitProvider);
            }
            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return await LoadYaml(context, file, callback, gitCommitProvider);
            }

            Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));
            return await LoadJson(context, file, callback, gitCommitProvider);
        }

        private static (List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)
            LoadMarkdown(
            Context context, Document file, PageCallback callback, GitCommitProvider gitCommitProvider)
        {
            var errors = new List<Error>();
            var content = file.ReadText();
            GitUtility.CheckMergeConflictMarker(content, file.FilePath);

            var (yamlHeaderErrors, yamlHeader) = ExtractYamlHeader.Extract(file, context);
            errors.AddRange(yamlHeaderErrors);

            var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(file.Docset.Metadata.GetMetadata(file, yamlHeader));
            errors.AddRange(metaErrors);

            var (error, monikers) = file.Docset.Monikers.GetFileLevelMonikers(file, metadata.MonikerRange);
            errors.AddIfNotNull(error);

            // TODO: handle blank page
            var (html, markup) = Markup.ToHtml(
                content,
                file,
                (path, relativeTo) => Resolve.ReadFile(path, relativeTo, errors, callback.DependencyMapBuilder, gitCommitProvider),
                (path, relativeTo, resultRelativeTo) => Resolve.GetLink(path, relativeTo, resultRelativeTo, errors, callback.BuildChild, callback.DependencyMapBuilder, callback.BookmarkValidator),
                (uid, moniker) => Resolve.ResolveXref(uid, callback.XrefMap, file, callback?.DependencyMapBuilder, moniker),
                (rangeString) => file.Docset.Monikers.GetZoneMonikers(rangeString, monikers, errors),
                MarkdownPipelineType.ConceptualMarkdown);
            errors.AddRange(markup.Errors);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var htmlTitleDom = HtmlUtility.LoadHtml(markup.HtmlTitle);
            var title = yamlHeader.Value<string>("title") ?? HtmlUtility.GetInnerText(htmlTitleDom);
            var finalHtml = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);

            var model = new PageModel
            {
                Content = finalHtml,
                Title = title,
                RawTitle = markup.HtmlTitle,
                WordCount = wordCount,
                Monikers = monikers,
            };

            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(htmlTitleDom)).ToHashSet();

            callback.BookmarkValidator.AddBookmarks(file, bookmarks);

            return (errors, Schema.Conceptual, model, metadata);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadYaml(
            Context context, Document file, PageCallback callback, GitCommitProvider gitCommitProvider)
        {
            var (errors, token) = YamlUtility.Deserialize(file, context);

            return await LoadSchemaDocument(errors, token, file, callback, gitCommitProvider);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadJson(
            Context context, Document file, PageCallback callback, GitCommitProvider gitCommitProvider)
        {
            var (errors, token) = JsonUtility.Deserialize(file, context);

            return await LoadSchemaDocument(errors, token, file, callback, gitCommitProvider);
        }

        private static async Task<(List<Error> errors, Schema schema, PageModel model, FileMetadata metadata)>
            LoadSchemaDocument(
            List<Error> errors, JToken token, Document file, PageCallback callback, GitCommitProvider gitCommitProvider)
        {
            // TODO: for backward compatibility, when #YamlMime:YamlDocument, documentType is used to determine schema.
            //       when everything is moved to SDP, we can refactor the mime check to Document.TryCreate
            var obj = token as JObject;
            var schema = file.Schema ?? Schema.GetSchema(obj?.Value<string>("documentType"));
            if (schema == null)
            {
                throw Errors.SchemaNotFound(file.Mime).ToException();
            }

            var (schemaViolationErrors, content) = JsonUtility.ToObjectWithSchemaValidation(token, schema.Type, transform: AttributeTransformer.Transform(errors, file, callback, gitCommitProvider));
            errors.AddRange(schemaViolationErrors);

            if (file.Docset.Legacy && schema.Attribute is PageSchemaAttribute)
            {
                content = await RazorTemplate.Render(schema.Name, content);
            }

            // TODO: add check before to avoid case failure
            var fileMetadata = obj?.Value<JObject>("metadata") ?? new JObject();
            var title = fileMetadata.Value<string>("title") ?? obj?.Value<string>("title");
            var (metaErrors, metadata) = JsonUtility.ToObjectWithSchemaValidation<FileMetadata>(file.Docset.Metadata.GetMetadata(file, fileMetadata));
            errors.AddRange(metaErrors);

            var model = new PageModel
            {
                Content = content,
                Title = title,
                RawTitle = file.Docset.Legacy ? $"<h1>{obj?.Value<string>("title")}</h1>" : null,
                Monikers = new List<string>(),
            };

            return (errors, schema, model, metadata);
        }
    }
}
