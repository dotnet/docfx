// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution,
            Action<Document> buildChild)
        {
            Debug.Assert(file.ContentType == ContentType.Markdown);

            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, buildChild);

            var htmlDom = HtmlUtility.LoadHtml(html);
            var content = markup.HasHtml ? htmlDom.StripTags().OuterHtml : html;
            var wordCount = HtmlUtility.CountWord(htmlDom);

            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

            // TODO: add check before to avoid case failure
            var (repoErrors, author, contributors, updatedAt) = contribution.GetContributorInfo(
                file,
                metadata.Value<string>("author"),
                metadata.Value<DateTime?>("update_date"));

            var title = metadata.Value<string>("title") ?? HtmlUtility.GetInnerText(markup.TitleHtml);

            var model = new PageModel
            {
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
                EditLink = contribution.GetEditLink(file),
                EnableContribution = file.Docset.Config.Contribution.Enabled,
            };

            // TODO: make build pure by not output using `context.Report/Write/Copy` here
            context.Report(file, markup.Errors.Concat(repoErrors));
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
        }
    }
}
