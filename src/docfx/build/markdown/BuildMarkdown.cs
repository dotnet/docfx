// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            GitRepoInfoProvider repo,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Markdown);

            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, buildChild);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            var wordCount = HtmlUtility.CountWord(html);
            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);
            var content = markup.HasHtml ? HtmlUtility.TransformHtml(document.DocumentNode, node => node.StripTags()) : html;
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;
            var (repoErrors, author, contributors, updatedAt) = repo.GetContributorInfo(file, GetInputAuthor(metadata));

            var model = new PageModel
            {
                Content = content,
                Metadata = metadata,
                Title = markup.Title,
                WordCount = wordCount,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
                Id = id,
                VersionIndependentId = versionIndependentId,
                Author = author,
                Contributors = contributors,
                UpdatedAt = updatedAt,
                EnableContribution = file.Docset.Config.Contribution.Enabled,
            };

            // TODO: make build pure by not output using `context.Report/Write/Copy` here
            context.Report(file, markup.Errors.Concat(repoErrors));
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
        }

        private static string GetInputAuthor(JObject metadata)
        {
            if (!metadata.TryGetValue("author", out var author))
                return null;

            var authorStr = ToString(author);
            if (string.IsNullOrEmpty(authorStr))
                return null;

            return authorStr;
        }

        private static string ToString(JToken obj)
        {
            if (obj == null)
                return null;
            if (!(obj is JValue jValue))
                return null;
            if (!(jValue.Value is string str))
                return null;

            return str;
        }
    }
}
