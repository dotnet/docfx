// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var markdown = file.ReadText();

            var (html, markup) = MarkdownUtility.Markup(markdown, file, context, ResolveHref);

            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);

            var model = new PageModel
            {
                Content = HtmlUtility.ProcessHtml(html),
                Metadata = new PageMetadata
                {
                    Title = markup.Title,
                    Metadata = metadata,
                },
            };

            context.WriteJson(model, file.OutputPath);
            return Task.CompletedTask;

            string ResolveHref(Document relativeTo, string href, Document resultRelativeTo)
            {
                var (link, buildItem) = relativeTo.TryResolveHref(href, resultRelativeTo);
                if (buildItem != null)
                {
                    buildChild(buildItem);
                }
                return link;
            }
        }
    }
}
