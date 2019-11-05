// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class DocumentProvider
    {
        private readonly Docset _docset;
        private readonly Docset _fallbackDocset;
        private readonly IReadOnlyDictionary<string, Docset> _dependencyDocsets;
        private readonly Input _input;
        private readonly TemplateEngine _templateEngine;
        private readonly ConcurrentDictionary<FilePath, Document> _documents = new ConcurrentDictionary<FilePath, Document>();

        public DocumentProvider(
            Docset docset, Docset fallbackDocset, Input input, RepositoryProvider repositoryProvider, TemplateEngine templateEngine)
        {
            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _dependencyDocsets = LoadDependencies(docset, repositoryProvider);
            _input = input;
            _templateEngine = templateEngine;
        }

        public Document GetDocument(FilePath path)
        {
            return _documents.GetOrAdd(path, GetDocumentCore);
        }

        private Document GetDocumentCore(FilePath path)
        {
            switch (path.Origin)
            {
                case FileOrigin.Fallback:
                    return Document.Create(_fallbackDocset, path, _input, _templateEngine);

                case FileOrigin.Dependency:
                    return Document.Create(_dependencyDocsets[path.DependencyName], path, _input, _templateEngine);

                default:
                    return Document.Create(_docset, path, _input, _templateEngine);
            }
        }

        private static Dictionary<string, Docset> LoadDependencies(Docset docset, RepositoryProvider repositoryProvider)
        {
            var config = docset.Config;
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);

            foreach (var (name, dependency) in config.Dependencies)
            {
                var (entry, repository) = repositoryProvider.GetRepositoryWithDocsetEntry(FileOrigin.Dependency, name);
                if (!string.IsNullOrEmpty(entry))
                {
                    result.TryAdd(name, new Docset(entry, docset.Locale, config, repository));
                }
            }

            return result;
        }
    }
}
