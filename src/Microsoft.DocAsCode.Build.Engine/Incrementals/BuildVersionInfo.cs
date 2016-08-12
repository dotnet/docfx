// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public class BuildVersionInfo
    {
        /// <summary>
        /// The version name of documents.
        /// </summary>
        public string VersionName { get; set; }
        /// <summary>
        /// The information for processors.
        /// </summary>
        public List<ProcessorInfo> Processors { get; } = new List<ProcessorInfo>();
        /// <summary>
        /// The file link for dependency (type is <see cref="DependencyGraph.Load(System.IO.TextReader)"/>).
        /// </summary>
        public string Dependency { get; set; }
        /// <summary>
        /// The file link for file attributes.
        /// e.g. last modified time, md5.
        /// </summary>
        public string Attributes { get; set; }
        /// <summary>
        /// The file link for build outputs (type is <see cref="BuildOutputs"/>).
        /// </summary>
        public string Output { get; set; }

    }
}
