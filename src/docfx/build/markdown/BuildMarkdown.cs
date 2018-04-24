// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task Build(Context context, Document file)
        {
            var markdown = file.ReadText();
            var model = new PageModel<string> { Content = Markup(markdown) };

            context.WriteJson(model, file.OutputPath);

            return Task.CompletedTask;
        }

        private static string Markup(string markdown)
        {
            return markdown;
        }
    }
}
