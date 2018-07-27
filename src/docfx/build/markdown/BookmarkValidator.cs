// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class BookmarkValidator
    {
        private readonly ConcurrentDictionary<Document, HashSet<string>> _bookmarksByFile = new ConcurrentDictionary<Document, HashSet<string>>();
        private readonly ConcurrentBag<(Document file, Document dependency, string bookmark)> _references = new ConcurrentBag<(Document file, Document dependency, string bookmark)>();

        public void AddBookmarkReference(Document file, Document reference, string fragment)
        {
            Debug.Assert(string.IsNullOrEmpty(fragment) || fragment[0] == '#');

            if (reference.ContentType == ContentType.Markdown && !string.IsNullOrEmpty(fragment))
            {
                var bookmark = fragment.Substring(1).Trim();
                if (!string.IsNullOrEmpty(bookmark))
                {
                    _references.Add((file, reference, bookmark));
                }
            }
        }

        public void AddBookmarks(Document file, HashSet<string> bookmarks)
        {
            _bookmarksByFile.TryAdd(file, bookmarks);
        }

        public List<Error> Validate()
        {
            var result = new List<Error>();

            foreach (var (file, reference, bookmark) in _references)
            {
                if (_bookmarksByFile.TryGetValue(reference, out var bookmarks) && bookmarks.Contains(bookmark))
                {
                    continue;
                }

                result.Add(Errors.BookmarkNotFound(file, reference, bookmark));
            }

            return result;
        }
    }
}
