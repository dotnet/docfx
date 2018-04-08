// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs
{
    internal class GitCommit
    {
        public string AuthorName { get; set; }

        public string AuthorEmail { get; set; }

        public string Sha { get; set; }

        public DateTimeOffset Time { get; set; }

        public override string ToString() => $"{Sha}: {AuthorName}, {AuthorEmail}";
    }
}
