// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public class BuildVersionInfo
    {
        /// <summary>
        /// The name of version.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// The file link for dependency.
        /// </summary>
        public string Dependency { get; set; }
        /// <summary>
        /// The file link for file attributes.
        /// </summary>
        public string Attributes { get; set; }
    }
}
