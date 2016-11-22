// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public static class EnvironmentContext
    {
        /// <summary>
        /// The directory path which contains docfx.json.
        /// </summary>
        public static string BaseDirectory { get; set; }
    }
}
