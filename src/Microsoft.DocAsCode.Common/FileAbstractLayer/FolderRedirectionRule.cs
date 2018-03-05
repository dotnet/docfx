// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public class FolderRedirectionRule
    {
        public FolderRedirectionRule(string from, string to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        public string From { get; set; }

        public string To { get; set; }
    }
}
