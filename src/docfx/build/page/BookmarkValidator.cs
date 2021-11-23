// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class BookmarkValidator
{
    private readonly ErrorBuilder _errors;

    private readonly Scoped<DictionaryBuilder<FilePath, HashSet<string>>> _bookmarksByFile = new();
    private readonly Scoped<ListBuilder<(FilePath file, FilePath dependency, string bookmark, bool selfBookmark, SourceInfo? source)>> _references = new();

    public BookmarkValidator(ErrorBuilder errors) => _errors = errors;

    public void AddBookmarkReference(FilePath file, FilePath reference, string? fragment, bool selfBookmark, SourceInfo? source)
    {
        if (!string.IsNullOrEmpty(fragment))
        {
            var bookmark = fragment[1..].Trim();
            if (!string.IsNullOrEmpty(bookmark))
            {
                Watcher.Write(() => _references.Value.Add((file, reference, bookmark, selfBookmark, source)));
            }
        }
    }

    public void AddBookmarks(FilePath file, HashSet<string> bookmarks)
    {
        Watcher.Write(() => _bookmarksByFile.Value.TryAdd(file, bookmarks));
    }

    public void Validate()
    {
        var bookmarksByFile = _bookmarksByFile.Value.AsDictionary();

        foreach (var (file, reference, bookmark, selfBookmark, source) in _references.Value.AsList())
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

            _errors.Add(Errors.Content.BookmarkNotFound(source, selfBookmark ? file : reference, bookmark, bookmarks));
        }
    }
}
