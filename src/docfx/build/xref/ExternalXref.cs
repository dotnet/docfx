// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class ExternalXref
    {
        public string Uid { get; set; } = "";

        public string? RepositoryUrl { get; set; }

        public int Count { get; set; }
    }
}
