// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<(IEnumerable<Error> errors, PageModel result, DependencyMap dependencies)> Build(
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Markdown);

            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, bookmarkValidator, buildChild);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var titleHtmlDom = HtmlUtility.LoadHtml(markup.TitleHtml);
            var content = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);
            var bookmarks = HtmlUtility.GetBookmarks(htmlDom).Concat(HtmlUtility.GetBookmarks(titleHtmlDom)).ToHashSet();

            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

            // TODO: add check before to avoid case failure
            var (repoErrors, author, contributors, updatedAt) = contribution.GetContributorInfo(
                file,
                metadata.Value<string>("author"),
                metadata.Value<DateTime?>("update_date"));

            var title = metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(titleHtmlDom);

            var (editUrl, contentUrl, commitUrl) = contribution.GetGitUrls(file);

            var model = new PageModel
            {
                PageType = "Conceptual",
                Content = content,
                Metadata = metadata,
                Title = title,
                TitleHtml = markup.TitleHtml,
                WordCount = wordCount,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
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

            bookmarkValidator.AddBookmarks(file, bookmarks);

            return Task.FromResult((markup.Errors.Concat(repoErrors), model, dependencyMapBuilder.Build()));
        }
    }
}
