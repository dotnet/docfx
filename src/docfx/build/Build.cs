// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs
{
    internal static class Build
    {
        public static async Task Run(string docsetPath)
        {
            return Run(Context.Create(docsetPath));
        }

        public static async Task Run(Context context, Docset docset)
        {
            var globbedFiles = GlobFiles(context, docset);

            await BuildFiles(context, globbedFiles);
        }

        private static List<Document> GlobFiles(Context context, Docset docset)
        {
            return new List<Document>();
        }

        private static async Task BuildFiles(Context context, List<Document> files)
        {
            await Task.Yield();

            await ParallelUtility.ForEach(files, BuildOneFile);

            Task BuildOneFile(Document file)
            {
                switch (file.ContentType)
                {
                    case ContentType.Asset:
                        return BuildAsset.Build(context, file);
                    case ContentType.Markdown:
                        return BuildMarkdown.Build(context, file);
                    case ContentType.SchemaDocument:
                        return BuildSchemaDocument.Build(context, file);
                    case ContentType.TableOfContents:
                        return BuildTableOfContents.Build(context, file);
                    default:
                        return Task.CompletedTask;
                }
            }
        }
    }
}
