// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal class Document
    {
        public Docset Docset { get; }

        public ContentType ContentType { get; }

        public FilePath FilePath { get; }

        public FilePath SitePath { get; }

        public UrlPath SiteUrl { get; }
    }
}
