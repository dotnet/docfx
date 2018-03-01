// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class PreviewConstants
    {
        public const string ServerHost = "localhost";
        public const string ServerPort = "4002";
        public const string TocMetadataName = "toc_rel";
        public const string PathPrefix = @"file:///";

        public static readonly ImmutableDictionary<string, string> References = new Dictionary<string, string>()
        {
            {"link", "href"},
            {"script", "src"},
            {"img", "src"}
        }.ToImmutableDictionary();
    }
}
