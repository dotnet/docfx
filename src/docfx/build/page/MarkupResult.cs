// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MarkupResult
    {
        public string HtmlTitle = "";

        public bool HasHtml;

        public List<Error> Errors = new List<Error>();

        public bool FirstBlockIsInclusionBlock = false;

        public ConcurrentBag<(Document xrefReferencedFile, bool uidInclusion)> XrefReferences = new ConcurrentBag<(Document xrefReferencedFile, bool uidInclusion)>();

        public bool HasTitle => !string.IsNullOrEmpty(HtmlTitle);
    }
}
