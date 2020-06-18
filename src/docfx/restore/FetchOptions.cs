// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal enum FetchOptions
    {
        /// <summary>
        /// Always fetch the latest content, without reading from disk cache.
        /// </summary>
        Latest = 0,

        /// <summary>
        /// Do not restore dependencies, throw `need-restore` when encounter missing dependencies.
        /// </summary>
        NoFetch = 0b0001,

        /// <summary>
        /// Default restore option: Only fetch latest content if it does not exist or read from disk cache.
        /// </summary>
        UseCache = 0b0010,
    }
}
