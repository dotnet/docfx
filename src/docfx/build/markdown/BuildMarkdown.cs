// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, buildChild);

            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);

            var content = markup.HasHtml ? HtmlUtility.TransformHtml(html, node => node.StripTags()) : html;

            var model = new PageModel
            {
                Content = content,
                Metadata = new PageMetadata
                {
                    Title = markup.Title,
                    Metadata = metadata,
                },
            };

            context.Report(file, markup.Errors);
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());
        }
    }
}
