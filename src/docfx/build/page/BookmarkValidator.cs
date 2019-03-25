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
        private readonly ConcurrentBag<(Document file, Document dependency, string bookmark, Range range)> _references = new ConcurrentBag<(Document file, Document dependency, string bookmark, Range range)>();

        public void AddBookmarkReference(Document file, Document reference, string fragment, Range range)
        {
            Debug.Assert(string.IsNullOrEmpty(fragment) || fragment[0] == '#');

            // only validate against markdown files
            if (reference.ContentType == ContentType.Page &&
                reference.FilePath.EndsWith(".md", PathUtility.PathComparison) &&
                !string.IsNullOrEmpty(fragment))
            {
                var bookmark = fragment.Substring(1).Trim();
                if (!string.IsNullOrEmpty(bookmark))
                {
                    _references.Add((file, reference, bookmark, range));
                }
            }
        }

        public void AddBookmarks(Document file, HashSet<string> bookmarks)
        {
            _bookmarksByFile.TryAdd(file, bookmarks);
        }

        public List<(Error error, Document file)> Validate()
        {
            var result = new List<(Error error, Document file)>();

            foreach (var (file, reference, bookmark, range) in _references)
            {
                if (_bookmarksByFile.TryGetValue(reference, out var bookmarks) && bookmarks.Contains(bookmark))
                {
                    continue;
                }
                result.Add(file == reference ? (Errors.InternalBookmarkNotFound(file, bookmark, bookmarks).WithRange(range), file) :
                                               (Errors.ExternalBookmarkNotFound(file, reference, bookmark, bookmarks).WithRange(range), file));
            }

            return result;
        }
    }
}
