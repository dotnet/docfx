// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;

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
        /// The file link for the BuildModel manifest file(type is <see cref="ModelManifest"/>).
        /// </summary>
        public string BuildModelManifestFile { get; set; }
        /// <summary>
        /// The file link for the PostBuildModel manifest file(type is <see cref="ModelManifest"/>).
        /// </summary>
        public string PostBuildModelManifestFile { get; set; }

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
        /// deserialized build model manifest
        /// </summary>
        [JsonIgnore]
        public ModelManifest BuildModelManifest { get; set; }
        /// <summary>
        /// deserialized postbuild model manifest
        /// </summary>
        [JsonIgnore]
        public ModelManifest PostBuildModelManifest { get; set; }
        #endregion

        internal void Load(string baseDir)
        {
            Dependency = IncrementalUtility.LoadDependency(Path.Combine(baseDir, DependencyFile));
            Attributes = IncrementalUtility.LoadIntermediateFile<IDictionary<string, FileAttributeItem>>(Path.Combine(baseDir, AttributesFile));
            Manifest = IncrementalUtility.LoadIntermediateFile<IEnumerable<ManifestItem>>(Path.Combine(baseDir, ManifestFile));
            XRefSpecMap = IncrementalUtility.LoadIntermediateFile<IDictionary<string, XRefSpec>>(Path.Combine(baseDir, XRefSpecMapFile));
            BuildModelManifest = IncrementalUtility.LoadIntermediateFile<ModelManifest>(Path.Combine(baseDir, BuildModelManifestFile));
            PostBuildModelManifest = IncrementalUtility.LoadIntermediateFile<ModelManifest>(Path.Combine(baseDir, PostBuildModelManifestFile));
        }

        internal void Save(string baseDir)
        {
            IncrementalUtility.SaveDependency(Path.Combine(baseDir, DependencyFile), Dependency);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, AttributesFile), Attributes);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, ManifestFile), Manifest);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, XRefSpecMapFile), XRefSpecMap);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, BuildModelManifestFile), BuildModelManifest);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, PostBuildModelManifestFile), PostBuildModelManifest);
        }
    }
}
