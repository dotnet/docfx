// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class PreviewConstants
    {
        public const string ConfigFile = "preview.json";
        public const string BuildSourceFolder = "articles";
        public const string BuildOutputSubfolder = "articles";
        public const string MarkupTagType = "article";
        public const string MarkupClassName = "content";
        public const string OutputFolder = "_site";
        public const string PageRefreshFunctionName = "refresh";
        public const string Port = "4001";
        public const string tocMetadataName = "toc_rel";
        public const string PathPrefix = @"file:///";

        public static readonly ImmutableDictionary<string, string> References = new Dictionary<string, string>()
        {
            {"link", "href"},
            {"script", "src"},
            {"img", "src"}
        }.ToImmutableDictionary();
    }
}
