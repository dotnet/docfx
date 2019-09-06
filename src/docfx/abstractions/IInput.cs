// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal interface IInput
    {
        /// <summary>
        /// Check if the specified file path exist.
        /// </summary>
        bool Exists(FilePath file);

        /// <summary>
        /// Try get the absolute path of the specified file if it exists physically on disk.
        /// Some file path like content from a bare git repo does not exist physically
        /// on disk but we can still read its content.
        /// </summary>
        bool TryGetPhysicalPath(FilePath file, out string physicalPath);

        /// <summary>
        /// Try get the file path in actual case.
        /// </summary>
        FilePath GetActualCase(FilePath file);

        /// <summary>
        /// Reads the specified file as a string.
        /// </summary>
        string ReadString(FilePath file);

        /// <summary>
        /// Open the specified file and read it as text.
        /// </summary>
        TextReader ReadText(FilePath file);

        /// <summary>
        /// List all the file path.
        /// </summary>
        FilePath[] ListFilesRecursive(FileOrigin origin, string dependencyName = null);
    }
}
