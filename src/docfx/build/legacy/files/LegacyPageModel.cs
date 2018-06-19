// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LegacyPageModel
    {
        public string Content { get; set; }

        public string OutputRootRelativePath { get; set; }

        public string ThemesRelativePathToOutputRoot { get; set; }

        public JObject RawMetadata { get; set; }

        public string PageMetadata { get; set; }
    }
}
