// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MarkupResult
    {
        public string HtmlTitle = "";

        public List<Error> Errors = new List<Error>();

        public bool FirstBlockIsInclusionBlock = false;

        public bool HasTitle => !string.IsNullOrEmpty(HtmlTitle);
    }
}
