// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, buildChild);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            var wordCount = HtmlUtility.CountWord(document.DocumentNode);
            var locale = file.Docset.Config.Locale;

            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);

            var content = markup.HasHtml ? HtmlUtility.TransformHtml(document.DocumentNode, node => node.StripTags()) : html;

            var model = new PageModel
            {
                Content = content,
                Metadata = new PageMetadata
                {
                    Title = markup.Title,
                    Metadata = metadata,
                },
                WordCount = wordCount,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
            };

            // TODO: make build pure by not output using `context.Report/Write/Copy` here
            context.Report(file, markup.Errors);
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
        }
    }
}
