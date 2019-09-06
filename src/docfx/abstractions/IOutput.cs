// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal interface IOutput
    {
        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        void WriteJson(object graph, string destRelativePath);

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        void WriteString(string value, string destRelativePath);

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        void Copy(string sourceRelativePath, string destRelativePath);

        void Delete(string destRelativePath);
    }
}
