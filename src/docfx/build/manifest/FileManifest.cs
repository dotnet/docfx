// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class FileManifest
    {
        public string SiteUrl { get; set; }

        public string OutputPath { get; set; }

        public string SourcePath { get; set; }

        public string Repo { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }
    }
}
