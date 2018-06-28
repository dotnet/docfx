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

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var wordCount = HtmlUtility.CountWord(html);
            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);
            var content = markup.HasHtml ? HtmlUtility.TransformHtml(document.DocumentNode, node => node.StripTags()) : html;
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

            // TODO: add check before to avoid case failure
            var (repoErrors, author, contributors, updatedAt) = repo.GetContributorInfo(
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
                EnableContribution = file.Docset.Config.Contribution.Enabled,
            };

            if (file.Docset.Config.Contribution.Enabled)
                model.EditLink = repo.GetEditLink(file);

            // TODO: make build pure by not output using `context.Report/Write/Copy` here
            context.Report(file, markup.Errors.Concat(repoErrors));
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
        }
    }
}
