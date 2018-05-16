// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task Build(Context context, Document file, TableOfContentsMap tocMap)
        {
            var markdown = file.ReadText();
            var (html, yamlHeader) = MarkdownUtility.Markup(markdown, file, context);
            var content = !string.IsNullOrEmpty(html) ? $"<div>{html.Trim()}</div>" : "";
            var model = new PageModel<string> { Content = content };

            context.WriteJson(model, file.OutputPath);

            var metadata = Metadata.FetchFromConfig(file);
            metadata.Merge(yamlHeader, JsonUtility.DefaultMergeSettings);

            if (metadata.HasValues)
                context.WriteJson(metadata, file.MetaOutputPath);

            return Task.CompletedTask;
        }
    }
}
