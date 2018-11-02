// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class PageCallback
    {
        public XrefMap XrefMap { get; }

        public BookmarkValidator BookmarkValidator { get; }

        public DependencyMapBuilder DependencyMapBuilder { get; }

        public Action<Document> BuildChild { get; }

        public PageCallback(XrefMap xrefMap, DependencyMapBuilder dependencyMapBuilder, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            XrefMap = xrefMap;
            DependencyMapBuilder = dependencyMapBuilder;
            BookmarkValidator = bookmarkValidator;
            BuildChild = buildChild;
        }
    }
}
