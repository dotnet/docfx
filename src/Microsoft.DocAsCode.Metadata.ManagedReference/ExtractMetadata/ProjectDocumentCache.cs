// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    internal class ProjectDocumentCache
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache =
            new ConcurrentDictionary<string, HashSet<string>>();

        public ProjectDocumentCache() { }

        public ProjectDocumentCache(IDictionary<string, List<string>> inputs)
        {
            if (inputs == null) return;
            foreach (var input in inputs)
            {
                HashSet<string> cacheValue = new HashSet<string>();
                if (input.Value != null)
                {
                    foreach (var item in input.Value)
                    {
                        cacheValue.Add(item.ToNormalizedFullPath());
                    }
                }
                if (cacheValue.Count > 0)
                {
                    _cache.TryAdd(input.Key.ToNormalizedFullPath(), cacheValue);
                }
            }
        }

        public IDictionary<string, List<string>> Cache =>
            _cache.ToDictionary(s => s.Key, s => s.Value.ToList());

        public IEnumerable<string> Documents =>
            _cache.Values.SelectMany(s => s.ToList()).Distinct();

        public void AddDocuments(IEnumerable<string> documents)
        {
            var key = documents.OrderBy(s => s).FirstOrDefault();
            AddDocuments(key, documents);
        }

        public void AddDocuments(string projectPath, IEnumerable<string> documents)
        {
            if (string.IsNullOrEmpty(projectPath) || documents == null || !documents.Any())
            {
                return;
            }
            var projectKey = projectPath.ToNormalizedFullPath();
            var documentCache = _cache.GetOrAdd(projectKey, s => new HashSet<string>());
            foreach (var document in documents)
            {
                documentCache.Add(document.ToNormalizedFullPath());
            }
        }

        public void AddDocument(string projectPath, string document)
        {
            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(document))
            {
                return;
            }
            var projectKey = projectPath.ToNormalizedFullPath();
            var documentCache = _cache.GetOrAdd(projectKey, s => new HashSet<string>());
            documentCache.Add(document.ToNormalizedFullPath());
        }

        public IEnumerable<string> GetDocuments(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }
            var projectKey = projectPath.ToNormalizedFullPath();
            _cache.TryGetValue(projectKey, out HashSet<string> documents);
            return documents.GetNormalizedFullPathList();
        }
    }
}
