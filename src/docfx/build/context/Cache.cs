// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Cache
    {
        private readonly ConcurrentDictionary<string, Lazy<(List<Error>, JToken)>> _tokenCache = new ConcurrentDictionary<string, Lazy<(List<Error>, JToken)>>();
        private readonly ConcurrentDictionary<string, Lazy<(List<Error> errors, TableOfContentsModel tocModel, List<Document> referencedFiles, List<Document> referencedTocs)>> _tocModelCache = new ConcurrentDictionary<string, Lazy<(List<Error>, TableOfContentsModel, List<Document>, List<Document>)>>();

        public (List<Error> errors, JToken token) LoadYamlFile(Document file)
            => _tokenCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JToken)>(() =>
            {
                var content = file.ReadText();
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                return YamlUtility.Parse(content, file.FilePath);
            })).Value;

        public (List<Error> errors, JToken token) LoadJsonFile(Document file)
            => _tokenCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JToken)>(() =>
            {
                var content = file.ReadText();
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                return JsonUtility.Parse(content, file.FilePath);
            })).Value;

        public (List<Error> errors, TableOfContentsModel tocModel, List<Document> referencedFiles, List<Document> referencedTocs) LoadTocModel(Context context, Document file)
            => _tocModelCache.GetOrAdd(
                file.FilePath,
                new Lazy<(List<Error>, TableOfContentsModel, List<Document>, List<Document>)>(
                    () => BuildTableOfContents.Load(context, file))).Value;

        private string GetKeyFromFile(Document file)
        {
            var filePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);
            return filePath + new FileInfo(filePath).LastWriteTime;
        }
    }
}
