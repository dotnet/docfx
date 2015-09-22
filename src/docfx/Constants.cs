// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;
    using System;
    using System.Diagnostics;
    using System.IO;

    internal static class Constants
    {
        public static Func<string, string> GetIndexFilePathFunc = new Func<string, string>(s => Path.Combine(s, "index.yml"));
        public const string ConfigFileName = "docfx.json";
        public const string WebsiteReferenceFolderName = "_ref_"; // Current OutputFolder
        public const string DefaultRootOutputFolderPath = "_site";
        public const string DefaultMetadataOutputFolderName = "_api_";
        public const string DefaultConceputalOutputFolderName = ""; // Current OutputFolder
    }
}
