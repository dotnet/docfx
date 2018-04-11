// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal class OutputConfig
    {
        public string Path { get; } = "_site";

        public string LogPath { get; } = "build.log";

        public bool Stable { get; }
    }
}
