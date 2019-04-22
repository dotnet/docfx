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
        private readonly ConcurrentDictionary<string, Lazy<(List<Error>, JObject)>> _metadataCache = new ConcurrentDictionary<string, Lazy<(List<Error>, JObject)>>();
        private readonly ConcurrentDictionary<string, Lazy<(List<Error>, TableOfContentsModel, List<(Document doc, string herf)>, List<Document>)>> _tocModelCache = new ConcurrentDictionary<string, Lazy<(List<Error>, TableOfContentsModel, List<(Document doc, string herf)>, List<Document>)>>();

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

        public (List<Error> errors, JObject metadata) ExtractMetadata(Document file)
            => _metadataCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JObject)>(() =>
            {
                using (var reader = new StreamReader(file.ReadStream()))
                {
                    return ExtractYamlHeader.Extract(reader, file.FilePath);
                }
            })).Value;

        public (List<Error>, TableOfContentsModel, List<(Document doc, string herf)>, List<Document>) LoadTocModel(Context context, Document file)
            => _tocModelCache.GetOrAdd(
                file.FilePath,
                new Lazy<(List<Error>, TableOfContentsModel, List<(Document doc, string herf)>, List<Document>)>(
                    () => BuildTableOfContents.Load(context, file))).Value;

        private string GetKeyFromFile(Document file)
        {
            var filePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);
            return filePath + new FileInfo(filePath).LastWriteTime;
        }
    }
}
