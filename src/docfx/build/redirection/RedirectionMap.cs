// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionMap
    {
        private readonly IReadOnlyDictionary<string, Document> _redirectionsBySourcePath;
        private readonly IReadOnlyDictionary<string, Document> _redirectionsByRedirectionUrl;

        public IEnumerable<Document> Files => _redirectionsBySourcePath.Values;

        private RedirectionMap(
            IReadOnlyDictionary<string, Document> redirectionsBySourcePath,
            IReadOnlyDictionary<string, Document> redirectionsByRedirectionUrl)
        {
            _redirectionsBySourcePath = redirectionsBySourcePath;
            _redirectionsByRedirectionUrl = redirectionsByRedirectionUrl;
        }

        public bool TryGetRedirection(string sourcePath, out Document file)
        {
            return _redirectionsBySourcePath.TryGetValue(sourcePath, out file);
        }

        public bool TryGetDocumentId(Document file, out (string id, string versionIndependentId) id)
        {
            if (_redirectionsByRedirectionUrl.TryGetValue(file.SiteUrl, out var doc))
            {
                id = TryGetDocumentId(doc, out var docId) ? docId : doc.Id;
                return true;
            }

            id = default;
            return false;
        }

        public static (List<Error> errors, RedirectionMap map) Create(Docset docset, Func<string, bool> glob)
        {
            var errors = new List<Error>();
            var redirections = new HashSet<Document>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections, checkRedirectTo: true);

            var redirectionsByRedirectionUrl = redirections
                .GroupBy(file => file.RedirectionUrl, PathUtility.PathComparer)
                .ToDictionary(group => group.Key, group => group.First(), PathUtility.PathComparer);

            errors.AddRange(redirections
                .GroupBy(file => file.RedirectionUrl)
                .Where(group => group.Count() > 1)
                .Select(group => Errors.RedirectionDocumentIdConflict(group, group.Key)));

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath, PathUtility.PathComparer);

            return (errors, new RedirectionMap(redirectionsBySourcePath, redirectionsByRedirectionUrl));

            void AddRedirections(Dictionary<string, SourceInfo<string>> items, bool checkRedirectTo = false)
            {
                foreach (var (path, redirectTo) in items)
                {
                    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectTo))
                    {
                        errors.Add(Errors.RedirectionIsNullOrEmpty(path, redirectTo));
                        continue;
                    }

                    if (checkRedirectTo && !redirectTo.Value.StartsWith('/'))
                    {
                        errors.Add(Errors.InvalidRedirectTo(redirectTo));
                        continue;
                    }

                    var pathToDocset = PathUtility.NormalizeFile(path);
                    var type = Document.GetContentType(pathToDocset);
                    if (type != ContentType.Page)
                    {
                        errors.Add(Errors.InvalidRedirection(redirectTo, path, type));
                    }
                    else if (!glob(pathToDocset))
                    {
                        errors.Add(Errors.RedirectionOutOfScope(redirectTo, pathToDocset));
                    }
                    else if (!redirections.Add(Document.Create(docset, pathToDocset, redirectTo)))
                    {
                        errors.Add(Errors.RedirectionConflict(pathToDocset));
                    }
                }
            }
        }
    }
}
