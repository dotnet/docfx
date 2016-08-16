// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Incrementals
{
    using System;
    using System.Collections.Generic;

    public class BuildInfo
    {
        public const string FileName = "build.info";

        /// <summary>
        /// The build start time for this build.
        /// </summary>
        public DateTime BuildStartTime { get; set; }
        /// <summary>
        /// The version of docfx.
        /// </summary>
        public string DocfxVersion { get; set; }
        /// <summary>
        /// The hash info for all plugins.
        /// </summary>
        public string PluginHash { get; set; }
        /// <summary>
        /// The hash info for templates.
        /// </summary>
        public string TemplateHash { get; set; }
        /// <summary>
        /// The hash info for configs.
        /// Include global metadata, file metadata.
        /// </summary>
        public string ConfigHash { get; set; }
        /// <summary>
        /// The file info for each version.
        /// </summary>
        public List<BuildVersionInfo> Versions { get; } = new List<BuildVersionInfo>();
    }
}
