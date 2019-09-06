// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IBookmarkValidator
    {
        void AddLink(FilePath declaringFile, FilePath targetFile, SourceInfo<string> url);

        void AddBookmarks(FilePath file, string[] bookmarks);

        void Validate();
    }
}
