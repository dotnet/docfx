﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

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
        /// The hash info for configs.
        /// Include global metadata, file metadata.
        /// </summary>
        public string ConfigHash { get; set; }
        /// <summary>
        /// The file link for dependency (type is <see cref="DependencyGraph.Load(System.IO.TextReader)"/>).
        /// </summary>
        public string DependencyFile { get; set; }
        /// <summary>
        /// The file link for file attributes.(type is <see cref="FileAttributes"/>).
        /// e.g. last modified time, md5.
        /// </summary>
        public string AttributesFile { get; set; }
        /// <summary>
        /// The file link for build outputs (type is <see cref="BuildOutputs"/>).
        /// </summary>
        public string OutputFile { get; set; }
        /// <summary>
        /// The file link for the manifest file(type is <see cref="T:Microsoft.DocAsCode.Plugins.Manifest"/>).
        /// </summary>
        public string ManifestFile { get; set; }
        /// <summary>
        /// The file link for the XRefMap file(type is <see cref="XRefMap"/>).
        /// </summary>
        public string XRefSpecMapFile { get; set; }
        /// <summary>
        /// The file link for the build message file (type is <see cref="BuildMessageInfo"/>).
        /// </summary>
        public string BuildMessageFile { get; set; }

        #region Deserialized content
        /// <summary>
        /// deserialized dependency graph
        /// </summary>
        [JsonIgnore]
        public DependencyGraph Dependency { get; set; }
        /// <summary>
        /// deserialized attributes
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, FileAttributeItem> Attributes { get; set; }
        /// <summary>
        /// deserialized manifestitems
        /// </summary>
        [JsonIgnore]
        public IEnumerable<ManifestItem> Manifest { get; set; }
        /// <summary>
        /// deserialized xrefspecmap
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, XRefSpec> XRefSpecMap { get; set; }
        /// <summary>
        /// deserialized build messages.
        /// </summary>
        [JsonIgnore]
        public BuildMessageInfo BuildMessage { get; } = new BuildMessageInfo();
        #endregion

        internal void Load(string baseDir)
        {
            Dependency = IncrementalUtility.LoadDependency(Path.Combine(baseDir, DependencyFile));
            Attributes = IncrementalUtility.LoadIntermediateFile<IDictionary<string, FileAttributeItem>>(Path.Combine(baseDir, AttributesFile));
            foreach (var processor in Processors)
            {
                processor.Load(baseDir);
            }
        }

        internal void Save(string baseDir)
        {
            IncrementalUtility.SaveDependency(Path.Combine(baseDir, DependencyFile), Dependency);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, AttributesFile), Attributes);
            foreach (var processor in Processors)
            {
                processor.Save(baseDir);
            }
        }
    }
}
