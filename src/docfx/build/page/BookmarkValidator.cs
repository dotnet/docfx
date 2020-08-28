// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class BookmarkValidator
    {
        private readonly ErrorBuilder _errors;

        private readonly DictionaryBuilder<FilePath, HashSet<string>> _bookmarksByFile = new DictionaryBuilder<FilePath, HashSet<string>>();
        private readonly ListBuilder<(FilePath file, FilePath dependency, string bookmark, bool isSelfBookmark, SourceInfo? source)> _references
                   = new ListBuilder<(FilePath file, FilePath dependency, string bookmark, bool isSelfBookmark, SourceInfo? source)>();

        public BookmarkValidator(ErrorBuilder errors)
        {
            _errors = errors;
        }

        public void AddBookmarkReference(FilePath file, FilePath reference, string? fragment, bool isSelfBookmark, SourceInfo? source)
        {
            if (!string.IsNullOrEmpty(fragment))
            {
                var bookmark = fragment.Substring(1).Trim();
                if (!string.IsNullOrEmpty(bookmark))
                {
                    _references.Add((file, reference, bookmark, isSelfBookmark, source));
                }
            }
        }

        public void AddBookmarks(FilePath file, HashSet<string> bookmarks)
        {
            _bookmarksByFile.TryAdd(file, bookmarks);
        }

        public void Validate()
        {
            var bookmarksByFile = _bookmarksByFile.AsDictionary();

            foreach (var (file, reference, bookmark, isSelfBookmark, source) in _references.AsList())
            {
                // #top is HTMl predefined URL, which points to the top of the page
                if (bookmark == "top")
                {
                    continue;
                }

                // Do not validate bookmark if the target file doesn't report bookmarks.
                // When the target file does not have bookmarks, it'll still report with an empty array.
                if (!bookmarksByFile.TryGetValue(reference, out var bookmarks))
                {
                    continue;
                }

                if (bookmarks.Contains(bookmark))
                {
                    continue;
                }

                _errors.Add(Errors.Content.BookmarkNotFound(source, isSelfBookmark ? file : reference, bookmark, bookmarks));
            }
        }
    }
}
