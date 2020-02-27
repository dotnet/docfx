// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class TemplateModel
    {
        public string Content { get; }

        public JObject RawMetadata { get; }

        public string PageMetadata { get; }

        public string ThemesRelativePathToOutputRoot { get; }

        public TemplateModel(string content, JObject rawMetadata, string pageMetadata, string themesRelativePathToOutputRoot)
        {
            Content = content;
            RawMetadata = rawMetadata;
            PageMetadata = pageMetadata;
            ThemesRelativePathToOutputRoot = themesRelativePathToOutputRoot;
        }
    }
}
