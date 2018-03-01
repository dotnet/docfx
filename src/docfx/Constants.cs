// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.IO;

    internal static class Constants
    {
        public static Func<string, string> GetIndexFilePathFunc = new Func<string, string>(s => Path.Combine(s, "index.yml"));
        public const string ConfigFileName = "docfx.json";
        public const string SupportedProjectName = "project.json";
        public const string ConfigFileExtension = ".json";
        public const string WebsiteReferenceFolderName = "_ref_"; // Current OutputFolder
        public const string DefaultRootOutputFolderPath = "_site";
        public const string DefaultMetadataOutputFolderName = "_api";
        public const string DefaultConceputalOutputFolderName = ""; // Current OutputFolder
        public const string DefaultTemplateName = "default";
        public const string EmbeddedTemplateFolderName = "Template";
    }
}
