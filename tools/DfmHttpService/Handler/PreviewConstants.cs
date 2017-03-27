// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class PreviewConstants
    {
        public const string ConfigFilename = "preview.json";
        public const string BuildSourceFolder = "articles";
        public const string BuildOutputSubfolder = "articles";
        public const string MarkupTagType = "article";
        public const string MarkupClassName = "content";
        public const string OutputFolder = "_site";
        public const string PageRefreshFunctionName = "refresh";
        public const string ServerPort = "4002";
        public const string NavigationPort = "4001";
        public const string TocMetadataName = "toc_rel";
        public const string PathPrefix = @"file:///";
        public const string DocfxTempPreviewFile = "docfxpreview.html";

        public static readonly ImmutableDictionary<string, string> References = new Dictionary<string, string>()
        {
            {"link", "href"},
            {"script", "src"},
            {"img", "src"}
        }.ToImmutableDictionary();
    }
}
