// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class BookmarkValidator
    {
        private readonly ErrorLog _errorLog;
        private readonly PublishModelBuilder _publishModelBuilder;

        private readonly DictionaryBuilder<Document, HashSet<string>> _bookmarksByFile = new DictionaryBuilder<Document, HashSet<string>>();
        private readonly ListBuilder<(Document file, Document dependency, string bookmark, bool isSelfBookmark, SourceInfo? source)>
            _references = new ListBuilder<(Document file, Document dependency, string bookmark, bool isSelfBookmark, SourceInfo? source)>();

        public BookmarkValidator(ErrorLog errorLog, PublishModelBuilder publishModelBuilder)
        {
            _errorLog = errorLog;
            _publishModelBuilder = publishModelBuilder;
        }

        public void AddBookmarkReference(Document file, Document reference, string? fragment, bool isSelfBookmark, SourceInfo? source)
        {
            if (reference.ContentType == ContentType.Page && !string.IsNullOrEmpty(fragment))
            {
                var bookmark = fragment.Substring(1).Trim();
                if (!string.IsNullOrEmpty(bookmark))
                {
                    _references.Add((file, reference, bookmark, isSelfBookmark, source));
                }
            }
        }

        public void AddBookmarks(Document file, HashSet<string> bookmarks)
        {
            _bookmarksByFile.TryAdd(file, bookmarks);
        }

        public void Validate()
        {
            var bookmarksByFile = _bookmarksByFile.ToDictionary();

            foreach (var (file, reference, bookmark, isSelfBookmark, source) in _references.ToList())
            {
                // #top is HTMl predefined URL, which points to the top of the page
                if (bookmark == "top")
                    continue;

                // Do not validate bookmark if the target file doesn't report bookmarks.
                // When the target file does not have bookmarks, it'll still report with an empty array.
                if (!bookmarksByFile.TryGetValue(reference, out var bookmarks))
                    continue;

                if (bookmarks.Contains(bookmark))
                    continue;

                var error = Errors.BookmarkNotFound(source, isSelfBookmark ? file : reference, bookmark, bookmarks);

                if (_errorLog.Write(error))
                {
                    _publishModelBuilder.ExcludeFromOutput(file);
                }
            }
        }
    }
}
