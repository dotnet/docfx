// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class Cache
    {
        private readonly ConcurrentDictionary<string,
            Lazy<(List<Error> errors, TableOfContentsModel tocModel, List<Document> referencedFiles, List<Document> referencedTocs)>>
            _tocModelCache = new ConcurrentDictionary<string, Lazy<(List<Error>, TableOfContentsModel, List<Document>, List<Document>)>>();

        private readonly Input _input;

        public Cache(Input input) => _input = input;

        public (List<Error> errors, TableOfContentsModel tocModel, List<Document> referencedFiles, List<Document> referencedTocs)
            LoadTocModel(Context context, Document file)
            => _tocModelCache.GetOrAdd(
                file.FilePath.Path,
                new Lazy<(List<Error>, TableOfContentsModel, List<Document>, List<Document>)>(
                    () => TableOfContentsParser.Load(context, file))).Value;
    }
}
