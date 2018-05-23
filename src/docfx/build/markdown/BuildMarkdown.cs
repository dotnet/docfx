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

            var content = !string.IsNullOrEmpty(html) ? $"<div>{html}</div>" : "";

            var model = new PageModel
            {
                Content = content,
                Metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), yamlHeader),
            };

            context.WriteJson(model, file.OutputPath);
            return Task.CompletedTask;
        }
    }
}
