// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static Task Build(Context context, Document file)
        {
            throw new NotImplementedException();
        }

        public static async Task<TableOfContentsMap> BuildTocMap(Context context, List<Document> files)
        {
            Debug.Assert(files != null);

            var builder = new TableOfContentsMapBuilder();
            var tocFiles = files.Where(f => f.ContentType == ContentType.TableOfContents);
            if (!tocFiles.Any())
            {
                return builder.Build();
            }

            await ParallelUtility.ForEach(tocFiles, file => BuildTocMap(context, file, builder));

            return builder.Build();
        }

        private static Task BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            Debug.Assert(tocMapBuilder != null);
            Debug.Assert(fileToBuild != null);

            var (tocModel, referencedDocuments, referencedTocs) = Load(fileToBuild);

            tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);

            return Task.CompletedTask;
        }

        private static (TableOfContentsModel tocModel, List<Document> referencedDocument, List<Document> referencedTocs) Load(Document fileToBuild)
        {
            throw new NotImplementedException();
        }
    }
}
