// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System.Collections.Generic;

    public static class PreviewConstants
    {
        public const string ConfigFile = "preview.json";
        public const string OutPutFolder = "_site";
        public const string MarkupResultLocation = "article";
        public const string Port = "8080";
        public const string PathPrefix = @"file:///";

        public static readonly Dictionary<string, string> References = new Dictionary<string, string>()
        {
            {"link", "href"},
            {"script", "src"},
            {"img", "src"}
        };
    }
}
