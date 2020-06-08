// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapItem
    {
        public string Url { get; }

        public string OutputPath { get; }

        public MonikerList Monikers { get; }

        public string SourcePath { get; }

        public PublishUrlMapItem(string url, string outputPath, MonikerList monikers, string sourcePath)
        {
            Url = url;
            OutputPath = outputPath;
            Monikers = monikers;
            SourcePath = sourcePath;
        }
    }
}
