// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class AttributeTransformerCallback
    {
        public XrefMap XrefMap { get; }

        public DependencyMapBuilder DependencyMap { get; }

        public BookmarkValidator BookmarkValidator { get; }

        public Action<Document> BuildChild { get; }

        public AttributeTransformerCallback(XrefMap xrefMap, DependencyMapBuilder dependencyMap, BookmarkValidator bookmarkValidator, Action<Document> buildChild)
        {
            XrefMap = xrefMap;
            DependencyMap = dependencyMap;
            BookmarkValidator = bookmarkValidator;
            BuildChild = buildChild;
        }
    }
}
