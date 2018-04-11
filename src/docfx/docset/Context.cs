// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal class Context
    {
        public IFileSystem FileSystem { get; }

        public ILogger Log { get; }

        public Context(IFileSystem fileSystem, ILogger log)
        {
            FileSystem = fileSystem;
            Log = log;
        }

        public static Context Create(string docsetPath)
        {

        }
    }
}
