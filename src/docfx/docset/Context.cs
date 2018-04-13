// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        /// <summary>
        /// Gets the file system abstraction.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets the logger to write logs, report diagnostics and progress.
        /// </summary>
        public ILog Log { get; }

        public Context(IFileSystem fileSystem, ILog log)
        {
            FileSystem = fileSystem;
            Log = log;
        }
    }
}
