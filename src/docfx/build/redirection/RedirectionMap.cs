// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public bool TryGetRedirectionUrl(string sourcePath, out string redirectionUrl)
        {
            if (_redirectionsBySourcePath.TryGetValue(sourcePath, out var file))
            {
                redirectionUrl = file.RedirectionUrl;
                return true;
            }
            redirectionUrl = null;
            return false;
        }

        public bool TryGetDocumentId(Document file, out (string id, string versionIndependentId) id)
        {
            if (_redirectionsByRedirectionUrl.TryGetValue(file.SiteUrl, out var docId))
            {
                id = docId.Id;
                return true;
            }

            id = default;
            return false;
        }

        public static (List<Error> errors, RedirectionMap map) Create(Docset docset)
        {
            var errors = new List<Error>();
            var redirections = new HashSet<Document>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections);

            var redirectionsByRedirectionUrl = redirections
                .GroupBy(file => file.RedirectionUrl, PathUtility.PathComparer)
                .ToDictionary(group => group.Key, group => group.First(), PathUtility.PathComparer);

            errors.AddRange(redirections
                .GroupBy(file => file.RedirectionUrl)
                .Where(group => group.Count() > 1)
                .Select(group => Errors.RedirectionDocumentIdConflict(group, group.Key)));

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutDocumentId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath, PathUtility.PathComparer);

            return (errors, new RedirectionMap(redirectionsBySourcePath, redirectionsByRedirectionUrl));

            void AddRedirections(Dictionary<string, string> items)
            {
                foreach (var (pathToDocset, redirectTo) in items)
                {
                    var (error, document) = Document.TryCreate(docset, pathToDocset, redirectTo);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                    else
                    {
                        if (!redirections.Add(document))
                        {
                            errors.Add(Errors.RedirectionConflict(pathToDocset));
                        }
                    }
                }
            }
        }
    }
}
