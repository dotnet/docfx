// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildTableOfContents
    {
        public static Task Build(Context context, Document file)
        {
            throw new NotImplementedException();
        }

        public static Task BuildTocMap(Context context, Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
        {
            Debug.Assert(tocMapBuilder != null);
            Debug.Assert(fileToBuild != null);

            var (tocModel, referencedDocuments, referencedTocs) = Load(fileToBuild);

            tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);

            return Task.FromResult(0);
        }

        private static (TableOfContentsModel tocModel, List<Document> referencedDocument, List<Document> referencedTocs) Load(Document fileToBuild)
        {
            throw new NotImplementedException();
        }
    }
}
