// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var markdown = file.ReadText();

            var (html, markup) = MarkdownUtility.Markup(
                markdown,
                file,
                context,
                (a, b, c) =>
                {
                    // resolve href link
                    var (link, buildItem) = Resolve.TryResolveHref(a, b, c);
                    if (buildItem != null)
                    {
                        buildChild(buildItem);
                    }

                    return link;
                });

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            var wordCount = HtmlUtility.CountWord(document.DocumentNode);
            var locale = file.Docset.Config.Locale;

            var metadata = JsonUtility.Merge(
                Metadata.GenerateRawMetadata(file, document.DocumentNode, locale, wordCount),
                Metadata.GetFromConfig(file),
                markup.Metadata);

            var model = new PageModel
            {
                Content = HtmlUtility.ProcessHtml(html),
                Metadata = new PageMetadata
                {
                    Title = markup.Title,
                    Metadata = metadata,
                },
                WordCount = wordCount,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
            };

            context.WriteJson(model, file.OutputPath);
            return Task.CompletedTask;
        }
    }
}
