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

        public (string id, string versionIndependentId) GetDocumentId(Document file)
        {
            return _redirectionsByRedirectionUrl.TryGetValue(file.SiteUrl, out var source) ? source.Id : file.Id;
        }

        public static (List<DocfxException> errors, RedirectionMap map) Create(Docset docset)
        {
            var errors = new List<DocfxException>();
            var redirections = new HashSet<Document>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections);

            var redirectionsByRedirectionUrl = redirections
                .GroupBy(file => file.RedirectionUrl)
                .ToDictionary(group => group.Key, group => group.First());

            errors.AddRange(redirections
                .GroupBy(file => file.RedirectionUrl)
                .Where(group => group.Count() > 1)
                .Select(group => Errors.RedirectionDocumentIdConflict(group, group.Key)));

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath);

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
